using Backend.DTOs;
using Backend.DTOs.Auth;

namespace Backend.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request);
        Task<LoginResponse> GoogleRegisterAsync(GoogleRegisterRequest request);
        Task SendOtpAsync(RegisterRequest request);
        Task<LoginResponse> VerifyOtpAndRegisterAsync(VerifyOtpRequest request);
        Task<TokenModel> RefreshTokenAsync(TokenModel request);
        Task ForgotPasswordAsync(ForgotPasswordRequest request);
        Task ResetPasswordAsync(ResetPasswordRequest request);
        Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task LogoutAsync(int userId);
    }
}
