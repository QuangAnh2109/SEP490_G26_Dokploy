using System.Security.Cryptography;
using Backend.Common.Options;
using Backend.Constants;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Backend.Services.Implements;

public sealed class RefreshTokenStore(
    IConnectionMultiplexer redis,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider,
    ILogger<RefreshTokenStore> logger) : IRefreshTokenStore
{
    private const int RawTokenByteLength = 32;
    private const int UserAgentMaxLength = 512;

    private readonly JwtOptions _options = options.Value;

    private IDatabase Db => redis.GetDatabase(RedisKeys.AuthDb);

    public async Task<(string RawToken, DateTimeOffset ExpiresAt)> IssueAsync(
        int userId,
        string jti,
        string? ip,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddDays(_options.RefreshTokenDays);
        var ttl = expiresAt - now;

        var rawBytes = RandomNumberGenerator.GetBytes(RawTokenByteLength);
        var rawToken = Base64UrlEncode(rawBytes);
        var hash = Sha256Hex(rawToken);

        var tokenKey = Keys.Token(userId, jti);
        var hashKey = Keys.Hash(hash);
        var userSetKey = Keys.UserSet(userId);

        var ua = Truncate(userAgent ?? string.Empty, UserAgentMaxLength);

        var tran = Db.CreateTransaction();
        _ = tran.HashSetAsync(tokenKey, new HashEntry[]
        {
            new("hash", hash),
            new("issuedAt", now.ToUnixTimeSeconds()),
            new("ua", ua),
            new("ip", ip ?? string.Empty),
            new("expiresAt", expiresAt.ToUnixTimeSeconds())
        });
        _ = tran.KeyExpireAsync(tokenKey, ttl);
        _ = tran.StringSetAsync(hashKey, $"{userId}|{jti}", ttl);
        _ = tran.SetAddAsync(userSetKey, jti);
        _ = tran.KeyExpireAsync(userSetKey, ttl);

        var committed = await tran.ExecuteAsync();
        if (!committed)
        {
            logger.LogError("Refresh token MULTI/EXEC commit failed for user {UserId} jti {Jti}", userId, jti);
            throw new InvalidOperationException("Failed to persist refresh token to Redis.");
        }

        return (rawToken, expiresAt);
    }

    public async Task<RefreshTokenValidation?> ConsumeAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var hash = Sha256Hex(rawToken);
        var hashKey = Keys.Hash(hash);

        var owner = await Db.StringGetAsync(hashKey);
        if (owner.IsNull)
        {
            return null;
        }

        var ownerStr = owner.ToString();
        var sep = ownerStr.IndexOf('|');
        if (sep <= 0 || !int.TryParse(ownerStr[..sep], out var userId))
        {
            return null;
        }
        var jti = ownerStr[(sep + 1)..];

        var tokenKey = Keys.Token(userId, jti);
        var userSetKey = Keys.UserSet(userId);

        var tran = Db.CreateTransaction();
        tran.AddCondition(Condition.HashEqual(tokenKey, "hash", hash));
        var delTokenTask = tran.KeyDeleteAsync(tokenKey);
        var delHashTask = tran.KeyDeleteAsync(hashKey);
        var sremTask = tran.SetRemoveAsync(userSetKey, jti);

        var committed = await tran.ExecuteAsync();
        if (committed)
        {
            await Task.WhenAll(delTokenTask, delHashTask, sremTask);
            return new RefreshTokenValidation(userId, jti);
        }

        // Hash condition fail = token đã consume → replay attempt → revoke all để force re-login mọi device.
        logger.LogWarning("Refresh token replay detected for user {UserId} jti {Jti} — revoking all sessions", userId, jti);
        await RevokeAllAsync(userId, cancellationToken);
        return null;
    }

    public async Task RevokeAsync(int userId, string jti, CancellationToken cancellationToken = default)
    {
        var tokenKey = Keys.Token(userId, jti);

        var storedHash = await Db.HashGetAsync(tokenKey, "hash");

        var batch = Db.CreateBatch();
        var pending = new List<Task>(3);

        if (storedHash.HasValue)
        {
            pending.Add(batch.KeyDeleteAsync(Keys.Hash(storedHash.ToString())));
        }
        pending.Add(batch.KeyDeleteAsync(tokenKey));
        pending.Add(batch.SetRemoveAsync(Keys.UserSet(userId), jti));

        batch.Execute();
        await Task.WhenAll(pending);
    }

    /// <summary>
    /// Revoke tất cả refresh token của user. Best-effort: nếu có IssueAsync chạy đồng thời
    /// trong window giữa SetMembersAsync và batch delete, jti mới có thể survive — nhưng vẫn
    /// tự expire qua TTL (RefreshTokenDays). Race window thực tế dưới 1ms; logout/reset password
    /// là tác vụ hiếm nên trade-off chấp nhận được.
    /// </summary>
    public async Task RevokeAllAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userSetKey = Keys.UserSet(userId);

        var jtis = await Db.SetMembersAsync(userSetKey);
        if (jtis.Length == 0)
        {
            return;
        }

        var hashTasks = jtis
            .Select(jti => Db.HashGetAsync(Keys.Token(userId, jti.ToString()), "hash"))
            .ToList();
        await Task.WhenAll(hashTasks);

        var batch = Db.CreateBatch();
        var pending = new List<Task>(jtis.Length * 2 + 1);
        for (int i = 0; i < jtis.Length; i++)
        {
            var jti = jtis[i].ToString();
            var storedHash = hashTasks[i].Result;

            if (storedHash.HasValue)
            {
                pending.Add(batch.KeyDeleteAsync(Keys.Hash(storedHash.ToString())));
            }
            pending.Add(batch.KeyDeleteAsync(Keys.Token(userId, jti)));
        }
        pending.Add(batch.KeyDeleteAsync(userSetKey));
        batch.Execute();

        await Task.WhenAll(pending);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string Sha256Hex(string value)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static class Keys
    {
        public static string Token(int userId, string jti) => $"{RedisKeys.RefreshTokenPrefix}{userId}:{jti}";
        public static string Hash(string hash) => $"{RedisKeys.RefreshTokenHashPrefix}{hash}";
        public static string UserSet(int userId) => $"{RedisKeys.RefreshTokenUserPrefix}{userId}";
    }
}
