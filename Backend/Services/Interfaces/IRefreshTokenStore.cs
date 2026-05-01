namespace Backend.Services.Interfaces;

public interface IRefreshTokenStore
{
    Task<(string RawToken, DateTimeOffset ExpiresAt)> IssueAsync(
        int userId,
        string jti,
        string? ip,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<RefreshTokenValidation?> ConsumeAsync(string rawToken, CancellationToken cancellationToken = default);

    Task RevokeAsync(int userId, string jti, CancellationToken cancellationToken = default);

    Task RevokeAllAsync(int userId, CancellationToken cancellationToken = default);
}

public sealed record RefreshTokenValidation(int UserId, string Jti);
