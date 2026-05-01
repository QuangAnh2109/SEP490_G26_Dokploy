using Backend.Common.Models;
using Backend.DTOs;
using Backend.DTOs.Auth;

namespace Backend.Services.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
    Task<Result<LoginResponse>> GoogleLoginAsync(GoogleLoginRequest request);
    Task<Result<LoginResponse>> GoogleRegisterAsync(GoogleRegisterRequest request);
    Task<Result<LoginResponse>> GoogleCompleteProfileAsync(GoogleCompleteProfileRequest request);
    Task<Result> SendOtpAsync(RegisterRequest request);
    Task<Result> ResendOtpAsync(string email);
    Task<Result<LoginResponse>> VerifyOtpAndRegisterAsync(VerifyOtpRequest request);
    Task<Result> RefreshTokenAsync();
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request);
    Task<Result> LogoutAsync(int userId, string jti);
    Task<Result> ChangePasswordFirstLoginAsync(int userId, ChangePasswordFirstLoginRequest request);
}
