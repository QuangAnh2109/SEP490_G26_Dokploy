using System.Text.Json;
using Backend.Constants;
using Backend.DTOs;
using Backend.DTOs.Auth;
using Backend.Services.Interfaces;
using StackExchange.Redis;

namespace Backend.Services.Implements;

public sealed class RedisOtpStore(IConnectionMultiplexer redis) : IOtpStore
{
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(10);

    private IDatabase Db => redis.GetDatabase(RedisKeys.AuthDb);

    public Task SetRegistrationOtpAsync(string email, RegisterRequest request, string otp, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(new OtpCacheEntry(request, otp));
        return Db.StringSetAsync(BuildRegKey(email), json, OtpTtl);
    }

    public async Task<OtpCacheEntry?> GetRegistrationOtpAsync(string email, CancellationToken ct = default)
    {
        var value = await Db.StringGetAsync(BuildRegKey(email));
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<OtpCacheEntry>(value!);
    }

    public Task RemoveRegistrationOtpAsync(string email, CancellationToken ct = default) =>
        Db.KeyDeleteAsync(BuildRegKey(email));

    public Task SetResetOtpAsync(string email, string otp, CancellationToken ct = default) =>
        Db.StringSetAsync(BuildResetKey(email), otp, OtpTtl);

    public async Task<string?> GetResetOtpAsync(string email, CancellationToken ct = default)
    {
        var value = await Db.StringGetAsync(BuildResetKey(email));
        return value.HasValue ? value.ToString() : null;
    }

    public Task RemoveResetOtpAsync(string email, CancellationToken ct = default) =>
        Db.KeyDeleteAsync(BuildResetKey(email));

    private static string BuildRegKey(string email) =>
        $"{RedisKeys.OtpPrefix}reg:{email.ToLowerInvariant()}";

    private static string BuildResetKey(string email) =>
        $"{RedisKeys.OtpPrefix}reset:{email.ToLowerInvariant()}";
}
