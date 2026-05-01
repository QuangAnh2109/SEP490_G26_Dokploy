using Backend.Common;
using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Common.Options;
using Backend.Constants;
using Backend.DTOs;
using Backend.DTOs.Auth;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Backend.Services.Implements;

public class AuthService(
    IAuthRepository authRepository,
    IConfiguration configuration,
    IEmailService emailService,
    IOtpStore otpStore,
    IJwtTokenService jwtTokenService,
    IRefreshTokenStore refreshTokenStore,
    IHttpContextAccessor httpContextAccessor,
    IOptions<AuthCookieOptions> cookieOptions) : IAuthService
{
    private readonly AuthCookieOptions _cookie = cookieOptions.Value;

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await authRepository.GetUserByEmailAsync(request.Email!);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return AuthErrors.InvalidCredentials;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return AuthErrors.InvalidCredentials;

        if (user.Status == UserStatus.Locked)
            return AuthErrors.AccountLocked;

        return await BuildLoginResponseAsync(user);
    }

    public async Task<Result<LoginResponse>> GoogleLoginAsync(GoogleLoginRequest request)
    {
        var payloadResult = await ValidateGoogleTokenAsync(request.IdToken!);
        if (payloadResult.IsFailure)
            return payloadResult.Error;

        var userEmail = payloadResult.Value.Email;
        var user = await authRepository.GetUserByEmailAsync(userEmail);

        if (user == null)
            return AuthErrors.InvalidCredentials;

        if (user.Status == UserStatus.Locked)
            return AuthErrors.AccountLocked;

        return await BuildLoginResponseAsync(user);
    }

    public async Task<Result> RefreshTokenAsync()
    {
        var ctx = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("RefreshTokenAsync requires HttpContext.");

        var rawToken = ctx.Request.Cookies[AuthCookieDefaults.RefreshName];
        if (string.IsNullOrEmpty(rawToken))
            return AuthErrors.RefreshTokenNotFound;

        var validation = await refreshTokenStore.ConsumeAsync(rawToken);
        if (validation == null)
            return AuthErrors.InvalidRefreshToken;

        var user = await authRepository.GetUserByIdAsync(validation.UserId);
        if (user == null)
            return AuthErrors.UserNotFound;

        var loginResult = await BuildLoginResponseAsync(user);
        return loginResult.IsFailure ? loginResult.Error : Result.Success();
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await authRepository.GetUserByEmailAsync(request.Email!);
        if (user == null)
            return Result.Success();

        var otp = GenerateOtp();
        await otpStore.SetResetOtpAsync(request.Email!, otp);

        var html = BuildOtpEmail(
            heading: "Đặt lại mật khẩu",
            intro: "Mã OTP để đặt lại mật khẩu của bạn là:",
            otp: otp,
            extraNote: "Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này.");

        await emailService.SendEmailAsync(request.Email!, "Mã Xác Thực Đặt Lại Mật Khẩu - Math Test Creator", html);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var cachedOtp = await otpStore.GetResetOtpAsync(request.Email!);
        if (string.IsNullOrEmpty(cachedOtp))
            return AuthErrors.OtpExpired;

        if (cachedOtp != request.OtpCode)
            return AuthErrors.OtpInvalid;

        await otpStore.RemoveResetOtpAsync(request.Email!);

        var user = await authRepository.GetUserByEmailAsync(request.Email!);
        if (user == null)
            return AuthErrors.UserNotFound;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await authRepository.UpdateUserAsync(user);

        await refreshTokenStore.RevokeAllAsync(user.UserId);
        ClearAuthCookies();

        return Result.Success();
    }

    public async Task<Result> LogoutAsync(int userId, string jti)
    {
        await refreshTokenStore.RevokeAsync(userId, jti);
        ClearAuthCookies();
        return Result.Success();
    }

    public async Task<Result> ChangePasswordFirstLoginAsync(int userId, ChangePasswordFirstLoginRequest request)
    {
        var user = await authRepository.GetUserByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return AuthErrors.UserNotFound;

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return AuthErrors.CurrentPasswordWrong;

        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
            return AuthErrors.NewPasswordSameAsOld;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        await authRepository.UpdateUserAsync(user);

        await refreshTokenStore.RevokeAllAsync(user.UserId);

        var loginResult = await BuildLoginResponseAsync(user);
        return loginResult.IsFailure ? loginResult.Error : Result.Success();
    }

    private async Task<Result<LoginResponse>> BuildLoginResponseAsync(User user)
    {
        if (!RoleIds.IsValid(user.RoleId))
            return AuthErrors.UnknownRole;

        var ctx = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("BuildLoginResponseAsync requires HttpContext.");

        var authProvider = string.IsNullOrEmpty(user.PasswordHash) ? "google" : "password";
        var (accessToken, jti, accessExpiresAt) = jwtTokenService.Issue(
            user.UserId,
            user.Email,
            user.RoleId.ToString(),
            authProvider,
            user.MustChangePassword);

        var ip = GetClientIp(ctx);
        var ua = ctx.Request.Headers.UserAgent.ToString();
        var (refreshToken, refreshExpiresAt) = await refreshTokenStore.IssueAsync(user.UserId, jti, ip, ua);

        CookieHelper.SetAccessCookie(ctx.Response, accessToken, accessExpiresAt, _cookie);
        CookieHelper.SetRefreshCookie(ctx.Response, refreshToken, refreshExpiresAt, _cookie);

        var roleName = user.Role?.Name ?? RoleIds.GetName(user.RoleId);

        return new LoginResponse
        {
            RoleName = roleName,
            Email = user.Email
        };
    }

    private void ClearAuthCookies()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx == null) return;
        CookieHelper.ClearAuthCookies(ctx.Response, _cookie);
    }

    private static string? GetClientIp(HttpContext ctx)
    {
        var cf = ctx.Request.Headers["CF-Connecting-IP"].ToString();
        if (!string.IsNullOrEmpty(cf)) return cf;

        var fwd = ctx.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrEmpty(fwd))
        {
            var first = fwd.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(first)) return first;
        }

        return ctx.Connection.RemoteIpAddress?.ToString();
    }

    private static string GenerateOtp() =>
        RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    private async Task<Result<GoogleJsonWebSignature.Payload>> ValidateGoogleTokenAsync(string idToken)
    {
        var clientId = configuration["Google:ClientId"];
        var settings = new GoogleJsonWebSignature.ValidationSettings();
        if (!string.IsNullOrEmpty(clientId) && clientId != "YOUR_GOOGLE_CLIENT_ID_HERE")
            settings.Audience = new[] { clientId };

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            if (payload == null)
                return AuthErrors.InvalidGoogleToken;

            return payload;
        }
        catch (InvalidJwtException)
        {
            return AuthErrors.InvalidGoogleToken;
        }
    }

    private static string BuildOtpEmail(string heading, string intro, string otp, string? extraNote = null)
    {
        var note = "Mã này chỉ được sử dụng một lần và sẽ hết hạn sau 10 phút.";
        if (!string.IsNullOrEmpty(extraNote))
            note += " " + extraNote;

        return $@"
            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2>{heading}</h2>
                <p>Chào bạn,</p>
                <p>{intro}</p>
                <h1 style='color: #2b6cb0; letter-spacing: 5px;'>{otp}</h1>
                <p>{note}</p>
            </div>";
    }
}
