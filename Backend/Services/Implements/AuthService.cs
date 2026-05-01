using Backend.Common;
using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Common.Options;
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
    IOptions<AuthCookieOptions> cookieOptions,
    TimeProvider timeProvider) : IAuthService
{
    private readonly AuthCookieOptions _cookie = cookieOptions.Value;

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await authRepository.GetUserByEmailAsync(request.Email!);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return AuthErrors.InvalidCredentials;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return AuthErrors.InvalidCredentials;

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
            return new LoginResponse { NeedsRegistration = true, Email = userEmail };

        var missing = GetMissingProfileFields(user);
        if (missing.Count > 0)
            return new LoginResponse { NeedsProfileCompletion = true, Email = userEmail, MissingFields = missing };

        return await BuildLoginResponseAsync(user);
    }

    public async Task<Result<LoginResponse>> GoogleRegisterAsync(GoogleRegisterRequest request)
    {
        var payloadResult = await ValidateGoogleTokenAsync(request.IdToken!);
        if (payloadResult.IsFailure)
            return payloadResult.Error;

        var userEmail = payloadResult.Value.Email;
        var existingUser = await authRepository.GetUserByEmailAsync(userEmail);
        if (existingUser != null)
            return AuthErrors.EmailAlreadyRegistered;

        var user = new User
        {
            PasswordHash = null,
            RoleId = request.RoleId ?? 0,
            Email = userEmail,
            SecurityStamp = timeProvider.GetUtcNow().UtcDateTime,
            FullName = (request.FullName ?? string.Empty).Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            StudentId = string.IsNullOrWhiteSpace(request.StudentId) ? null : request.StudentId.Trim()
        };

        await authRepository.AddUserAsync(user);
        return await BuildLoginResponseAsync(user);
    }

    public async Task<Result<LoginResponse>> GoogleCompleteProfileAsync(GoogleCompleteProfileRequest request)
    {
        var payloadResult = await ValidateGoogleTokenAsync(request.IdToken!);
        if (payloadResult.IsFailure)
            return payloadResult.Error;

        var userEmail = payloadResult.Value.Email;
        var user = await authRepository.GetUserByEmailAsync(userEmail);
        if (user == null)
            return AuthErrors.UserNotFound;

        user.FullName = (request.FullName ?? string.Empty).Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        if (user.RoleId == 2 && !string.IsNullOrWhiteSpace(request.StudentId))
            user.StudentId = request.StudentId.Trim();

        await authRepository.UpdateUserAsync(user);
        return await BuildLoginResponseAsync(user);
    }

    public async Task<Result> SendOtpAsync(RegisterRequest request)
    {
        var existingUser = await authRepository.GetUserByEmailAsync(request.Email!);
        if (existingUser != null)
            return AuthErrors.EmailAlreadyRegistered;

        var otp = GenerateOtp();
        await otpStore.SetRegistrationOtpAsync(request.Email!, request, otp);

        var html = BuildOtpEmail(
            heading: "Xác thực Email đăng ký",
            intro: "Mã OTP để hoàn tất đăng ký tài khoản của bạn là:",
            otp: otp);
        await emailService.SendEmailAsync(request.Email!, "Mã Xác Thực OTP - Math Test Creator", html);
        return Result.Success();
    }

    public async Task<Result> ResendOtpAsync(string email)
    {
        var entry = await otpStore.GetRegistrationOtpAsync(email);
        if (entry == null)
            return AuthErrors.OtpExpired;

        var newOtp = GenerateOtp();
        await otpStore.SetRegistrationOtpAsync(email, entry.Request, newOtp);

        var html = BuildOtpEmail(
            heading: "Xác thực Email đăng ký",
            intro: "Mã OTP mới để hoàn tất đăng ký tài khoản của bạn là:",
            otp: newOtp);
        await emailService.SendEmailAsync(email, "Mã Xác Thực OTP - Math Test Creator", html);
        return Result.Success();
    }

    public async Task<Result<LoginResponse>> VerifyOtpAndRegisterAsync(VerifyOtpRequest request)
    {
        var entry = await otpStore.GetRegistrationOtpAsync(request.Email!);
        if (entry == null)
            return AuthErrors.OtpExpired;

        if (entry.Otp != request.OtpCode)
            return AuthErrors.OtpInvalid;

        await otpStore.RemoveRegistrationOtpAsync(request.Email!);

        var regRequest = entry.Request;
        var existingUser = await authRepository.GetUserByEmailAsync(regRequest.Email!);
        if (existingUser != null)
            return AuthErrors.EmailAlreadyRegistered;

        var user = new User
        {
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(regRequest.Password),
            RoleId = regRequest.RoleId ?? 0,
            Email = regRequest.Email!,
            SecurityStamp = timeProvider.GetUtcNow().UtcDateTime,
            FullName = (regRequest.FullName ?? string.Empty).Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(regRequest.PhoneNumber) ? null : regRequest.PhoneNumber.Trim(),
            StudentId = string.IsNullOrWhiteSpace(regRequest.StudentId) ? null : regRequest.StudentId.Trim()
        };

        await authRepository.AddUserAsync(user);
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
        if (user.RoleId != 1 && user.RoleId != 2)
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

        var roleName = user.Role?.Name ?? (user.RoleId == 1 ? "Teacher" : user.RoleId == 2 ? "Student" : "Unknown");

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

    private static List<string> GetMissingProfileFields(User user)
    {
        var missing = new List<string>();
        var fullName = (user.FullName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(fullName) ||
            !System.Text.RegularExpressions.Regex.IsMatch(fullName, @"^[\p{L}\p{M}]+(?:\s+[\p{L}\p{M}]+)*$"))
        {
            missing.Add("FullName");
        }

        if (user.RoleId == 2)
        {
            var studentId = (user.StudentId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(studentId) ||
                !System.Text.RegularExpressions.Regex.IsMatch(studentId, @"^[A-Za-z]{2}\d{6}$"))
            {
                missing.Add("StudentId");
            }
        }

        return missing;
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
