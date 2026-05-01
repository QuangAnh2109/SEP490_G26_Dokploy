namespace Backend.Services.Interfaces;

public interface IJwtTokenService
{
    (string Token, string Jti, DateTimeOffset ExpiresAt) Issue(
        int userId,
        string email,
        string role,
        string authProvider,
        bool mustChangePassword);
}
