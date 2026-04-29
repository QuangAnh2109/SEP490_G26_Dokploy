using Backend.Common;
using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Constants;
using Backend.DTOs;
using Backend.DTOs.Auth;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend.Services.Implements;

public class AuthService(
    IAuthRepository authRepository,
    IConfiguration configuration,
    IEmailService emailService,
    IMemoryCache cache) : IAuthService
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await authRepository.GetUserByEmailAsync(request.Email!);

        // Merge null/google/wrong-password into single code to prevent enumeration
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return AuthErrors.InvalidCredentials;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return AuthErrors.InvalidCredentials;

        return BuildLoginResponse(user);
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

        return BuildLoginResponse(user);
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
            SecurityStamp = DateTime.UtcNow,
            FullName = (request.FullName ?? string.Empty).Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            StudentId = string.IsNullOrWhiteSpace(request.StudentId) ? null : request.StudentId.Trim()
        };

        await authRepository.AddUserAsync(user);
        return BuildLoginResponse(user);
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
        return BuildLoginResponse(user);
    }

    public async Task<Result> SendOtpAsync(RegisterRequest request)
    {
        var existingUser = await authRepository.GetUserByEmailAsync(request.Email!);
        if (existingUser != null)
            return AuthErrors.EmailAlreadyRegistered;

        var otp = new Random().Next(100000, 999999).ToString();
        var cacheKey = $"OTP_{request.Email}";
        _cache.Set(cacheKey, new { Request = request, Otp = otp }, TimeSpan.FromMinutes(10));

        var html = BuildOtpEmail(otp, isResend: false);
        await emailService.SendEmailAsync(request.Email!, "Mã Xác Thực OTP - Math Test Creator", html);
        return Result.Success();
    }

    public async Task<Result> ResendOtpAsync(string email)
    {
        var cacheKey = $"OTP_{email}";
        if (!_cache.TryGetValue(cacheKey, out dynamic? cacheData) || cacheData == null)
            return AuthErrors.OtpExpired;

        var regRequest = (RegisterRequest)cacheData!.Request;
        var newOtp = new Random().Next(100000, 999999).ToString();
        _cache.Set(cacheKey, new { Request = regRequest, Otp = newOtp }, TimeSpan.FromMinutes(10));

        var html = BuildOtpEmail(newOtp, isResend: true);
        await emailService.SendEmailAsync(email, "Mã Xác Thực OTP - Math Test Creator", html);
        return Result.Success();
    }

    public async Task<Result<LoginResponse>> VerifyOtpAndRegisterAsync(VerifyOtpRequest request)
    {
        var cacheKey = $"OTP_{request.Email}";
        if (!_cache.TryGetValue(cacheKey, out dynamic? cacheData) || cacheData == null)
            return AuthErrors.OtpExpired;

        if (cacheData!.Otp != request.OtpCode)
            return AuthErrors.OtpInvalid;

        _cache.Remove(cacheKey);

        RegisterRequest regRequest = cacheData.Request;
        var existingUser = await authRepository.GetUserByEmailAsync(regRequest.Email!);
        if (existingUser != null)
            return AuthErrors.EmailAlreadyRegistered;

        var user = new User
        {
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(regRequest.Password),
            RoleId = regRequest.RoleId ?? 0,
            Email = regRequest.Email!,
            SecurityStamp = DateTime.UtcNow,
            FullName = (regRequest.FullName ?? string.Empty).Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(regRequest.PhoneNumber) ? null : regRequest.PhoneNumber.Trim(),
            StudentId = string.IsNullOrWhiteSpace(regRequest.StudentId) ? null : regRequest.StudentId.Trim()
        };

        await authRepository.AddUserAsync(user);
        return BuildLoginResponse(user);
    }

    public async Task<Result<TokenModel>> RefreshTokenAsync(TokenModel request)
    {
        var principal = CreatePrincipalFromExpiredToken(request.RefreshToken, isRefreshToken: true);
        if (principal == null)
            return AuthErrors.InvalidRefreshToken;

        var userEmail = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "";
        var tokenSecurityStamp = principal.Claims.FirstOrDefault(c => c.Type == "AspNet.Identity.SecurityStamp")?.Value ?? "";

        var user = await authRepository.GetUserByEmailAsync(userEmail);
        if (user == null || user.SecurityStamp.ToString("o") != tokenSecurityStamp)
            return AuthErrors.InvalidRefreshToken;

        var tokenResult = GenerateJwtToken(user);
        if (tokenResult.IsFailure)
            return tokenResult.Error;

        return new TokenModel
        {
            AccessToken = tokenResult.Value,
            RefreshToken = GenerateRefreshTokenAsJwt(user)
        };
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await authRepository.GetUserByEmailAsync(request.Email!);
        if (user == null)
            return Result.Success(); // silent — no enumeration

        var otp = new Random().Next(100000, 999999).ToString();
        var cacheKey = $"RESET_OTP_{request.Email}";
        _cache.Set(cacheKey, otp, TimeSpan.FromMinutes(10));

        var html = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2>Đặt lại mật khẩu</h2>
                <p>Chào bạn,</p>
                <p>Mã OTP để đặt lại mật khẩu của bạn là:</p>
                <h1 style='color: #2b6cb0; letter-spacing: 5px;'>{otp}</h1>
                <p>Mã này chỉ được sử dụng một lần và sẽ hết hạn sau 10 phút. Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này.</p>
            </div>";

        await emailService.SendEmailAsync(request.Email!, "Mã Xác Thực Đặt Lại Mật Khẩu - Math Test Creator", html);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var cacheKey = $"RESET_OTP_{request.Email}";
        if (!_cache.TryGetValue(cacheKey, out string? cachedOtp) || string.IsNullOrEmpty(cachedOtp))
            return AuthErrors.OtpExpired;

        if (cachedOtp != request.OtpCode)
            return AuthErrors.OtpInvalid;

        _cache.Remove(cacheKey);

        var user = await authRepository.GetUserByEmailAsync(request.Email!);
        if (user == null)
            return AuthErrors.UserNotFound;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.SecurityStamp = DateTime.UtcNow;
        await authRepository.UpdateUserAsync(user);
        return Result.Success();
    }

    public async Task<Result> LogoutAsync(int userId)
    {
        var user = await authRepository.GetUserByIdAsync(userId);
        if (user != null)
        {
            user.SecurityStamp = DateTime.UtcNow;
            await authRepository.UpdateUserAsync(user);
        }

        return Result.Success();
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private Result<LoginResponse> BuildLoginResponse(User user)
    {
        var tokenResult = GenerateJwtToken(user);
        if (tokenResult.IsFailure)
            return tokenResult.Error;

        var roleName = user.Role?.Name ?? (user.RoleId == 1 ? "Teacher" : user.RoleId == 2 ? "Student" : "Unknown");

        return new LoginResponse
        {
            Token = tokenResult.Value,
            RefreshToken = GenerateRefreshTokenAsJwt(user),
            RoleName = roleName,
            Email = user.Email
        };
    }

    private Result<string> GenerateJwtToken(User user)
    {
        if (user.RoleId != 1 && user.RoleId != 2)
            return AuthErrors.UnknownRole;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.RoleId.ToString()),
            new("auth_provider", string.IsNullOrEmpty(user.PasswordHash) ? "google" : "password")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? ""));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshTokenAsJwt(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("AspNet.Identity.SecurityStamp", user.SecurityStamp.ToString("o"))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? ""));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var audience = configuration["Jwt:Audience"] + "_Refresh";
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

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

    private ClaimsPrincipal? CreatePrincipalFromExpiredToken(string? token, bool isRefreshToken = false)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? ""));
        var audience = isRefreshToken ? configuration["Jwt:Audience"] + "_Refresh" : configuration["Jwt:Audience"];
        var parameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuer = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = isRefreshToken
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
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

    private static string BuildOtpEmail(string otp, bool isResend)
    {
        var intro = isResend ? "Mã OTP mới để hoàn tất đăng ký tài khoản của bạn là:" : "Mã OTP để hoàn tất đăng ký tài khoản của bạn là:";
        return $@"
            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2>Xác thực Email đăng ký</h2>
                <p>Chào bạn,</p>
                <p>{intro}</p>
                <h1 style='color: #2b6cb0; letter-spacing: 5px;'>{otp}</h1>
                <p>Mã này chỉ được sử dụng một lần và sẽ hết hạn sau 10 phút.</p>
            </div>";
    }

    private readonly IMemoryCache _cache = cache;
}
