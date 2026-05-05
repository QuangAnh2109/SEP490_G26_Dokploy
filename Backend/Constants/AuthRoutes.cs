namespace Backend.Constants;

public static class AuthRoutes
{
    /// <summary>
    /// Phải khớp với route của AuthController.RefreshToken. Sai → cookie không gửi → flow vỡ silent.
    /// </summary>
    public const string RefreshTokenPath = "/api/auth/refresh-token";
}
