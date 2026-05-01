using Backend.Common;
using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.DTOs;
using Backend.DTOs.Auth;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Roles = RoleIds.Teacher + "," + RoleIds.Student)]
    public IActionResult GetMe() =>
        Ok(new
        {
            userId = currentUser.UserId,
            email = currentUser.Email,
            role = currentUser.Role,
            authProvider = User.FindFirstValue("auth_provider")
        });

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request) =>
        (await authService.LoginAsync(request)).ToActionResult(this);

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request) =>
        (await authService.GoogleLoginAsync(request)).ToActionResult(this);

    [HttpPost("google-register")]
    public async Task<IActionResult> GoogleRegister([FromBody] GoogleRegisterRequest request) =>
        (await authService.GoogleRegisterAsync(request)).ToActionResult(this);

    [HttpPost("google-complete-profile")]
    public async Task<IActionResult> GoogleCompleteProfile([FromBody] GoogleCompleteProfileRequest request) =>
        (await authService.GoogleCompleteProfileAsync(request)).ToActionResult(this);

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] RegisterRequest request) =>
        (await authService.SendOtpAsync(request)).ToActionResult(this);

    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request) =>
        (await authService.ResendOtpAsync(request.Email!)).ToActionResult(this);

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request) =>
        (await authService.VerifyOtpAndRegisterAsync(request)).ToActionResult(this);

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken() =>
        (await authService.RefreshTokenAsync()).ToActionResult(this);

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request) =>
        (await authService.ForgotPasswordAsync(request)).ToActionResult(this);

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request) =>
        (await authService.ResetPasswordAsync(request)).ToActionResult(this);

    [HttpPost("logout")]
    [Authorize(Roles = RoleIds.Teacher + "," + RoleIds.Student)]
    [AllowPasswordChange]
    public async Task<IActionResult> Logout()
    {
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (string.IsNullOrEmpty(jti))
            return Result.Failure(AuthErrors.MissingJti).ToActionResult(this);

        return (await authService.LogoutAsync(currentUser.UserId, jti)).ToActionResult(this);
    }

    [HttpPost("change-password-first-login")]
    [Authorize(Roles = RoleIds.Teacher + "," + RoleIds.Student)]
    [AllowPasswordChange]
    public async Task<IActionResult> ChangePasswordFirstLogin([FromBody] ChangePasswordFirstLoginRequest request) =>
        (await authService.ChangePasswordFirstLoginAsync(currentUser.UserId, request)).ToActionResult(this);
}
