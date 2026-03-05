using Backend.DTOs;
using Backend.DTOs.Auth;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Backend.Constants;

namespace Backend.Services.Implements
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public AuthService(IAuthRepository authRepository, IConfiguration configuration, IEmailService emailService, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _emailService = emailService;
            _cache = cache;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var user = await _authRepository.GetUserByEmailAsync(request.Email);

            if (user == null)
            {
                System.Console.WriteLine($"[AUTH_DEBUG] User with email '{request.Email}' is NULL when queried from DB. Checking precise length: {request.Email.Length}");
                throw new UnauthorizedAccessException(ErrorMessages.InvalidEmailOrPassword);
            }
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                System.Console.WriteLine($"[AUTH_DEBUG] Verification failed for '{request.Email}'. PasswordHash in DB was: '{user.PasswordHash}', requested password length: {request.Password.Length}");
                throw new UnauthorizedAccessException(ErrorMessages.InvalidEmailOrPassword);
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshTokenAsJwt(user);

            return new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                RoleName = user.Role?.Name ?? "User",
                Email = user.Email
            };
        }

        public async Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request)
        {
            var payload = await ValidateGoogleTokenAsync(request.IdToken);
            var userEmail = payload.Email;

            var user = await _authRepository.GetUserByEmailAsync(userEmail);

            if (user == null)
            {
                return new LoginResponse
                {
                    NeedsRegistration = true,
                    Email = userEmail
                };
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshTokenAsJwt(user);

            return new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                RoleName = user.Role?.Name ?? "User",
                Email = user.Email
            };
        }

        public async Task<LoginResponse> GoogleRegisterAsync(GoogleRegisterRequest request)
        {
            var payload = await ValidateGoogleTokenAsync(request.IdToken);
            var userEmail = payload.Email;

            var existingUser = await _authRepository.GetUserByEmailAsync(userEmail);
            if (existingUser != null)
            {
                throw new InvalidOperationException(ErrorMessages.UserAlreadyExists);
            }

            var user = new User
            {
                PasswordHash = Guid.NewGuid().ToString(),
                RoleId = request.RoleId,
                Email = userEmail,
                SecurityStamp = DateTime.UtcNow
            };

            await _authRepository.AddUserAsync(user);

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshTokenAsJwt(user);

            return new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                RoleName = user.Role?.Name ?? "User",
                Email = user.Email
            };
        }

        public async Task SendOtpAsync(RegisterRequest request)
        {
            // 1. Check if email already exists
            var existingUserByEmail = await _authRepository.GetUserByEmailAsync(request.Email);
            if (existingUserByEmail != null && existingUserByEmail.Email == request.Email)
                throw new InvalidOperationException(ErrorMessages.EmailAlreadyRegistered);

            // 2. Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // 3. Save Registration Info and OTP to Cache (expires in 10 minutes)
            var cacheKey = $"OTP_{request.Email}";
            var cacheData = new { Request = request, Otp = otp };
            _cache.Set(cacheKey, cacheData, TimeSpan.FromMinutes(10));

            // 4. Send Email
            var htmlMessage = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Xác thực Email đăng ký</h2>
                    <p>Chào bạn,</p>
                    <p>Mã OTP để hoàn tất đăng ký tài khoản của bạn là:</p>
                    <h1 style='color: #2b6cb0; letter-spacing: 5px;'>{otp}</h1>
                    <p>Mã này chỉ được sử dụng một lần và sẽ hết hạn sau 10 phút.</p>
                </div>";

            await _emailService.SendEmailAsync(request.Email, "Mã Xác Thực OTP - Math Test Creator", htmlMessage);
        }

        public async Task<LoginResponse> VerifyOtpAndRegisterAsync(VerifyOtpRequest request)
        {
            var cacheKey = $"OTP_{request.Email}";

            if (!_cache.TryGetValue(cacheKey, out dynamic? cacheData) || cacheData == null)
            {
                throw new UnauthorizedAccessException(ErrorMessages.OtpExpiredOrNotExists);
            }

            if (cacheData.Otp != request.OtpCode)
            {
                throw new UnauthorizedAccessException(ErrorMessages.InvalidOtp);
            }

            // ONE-TIME USE: Remove OTP from cache IMMEDIATELY upon verification
            _cache.Remove(cacheKey);

            // OTP valid, proceed to register
            RegisterRequest regRequest = cacheData.Request;

            var existingUser = await _authRepository.GetUserByEmailAsync(regRequest.Email);
            if (existingUser != null)
            {
                _cache.Remove(cacheKey); // Cleanup
                throw new InvalidOperationException(ErrorMessages.UserAlreadyExists);
            }

            var user = new User
            {
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(regRequest.Password),
                RoleId = regRequest.RoleId,
                Email = regRequest.Email,
                SecurityStamp = DateTime.UtcNow
            };

            await _authRepository.AddUserAsync(user);

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshTokenAsJwt(user);

            return new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                RoleName = user.Role?.Name ?? "User",
                Email = user.Email
            };
        }

        public async Task<TokenModel> RefreshTokenAsync(TokenModel request)
        {
            if (request == null || string.IsNullOrEmpty(request.RefreshToken))
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Parse existing Refresh Token
            var principal = CreatePrincipalFromExpiredToken(request.RefreshToken, isRefreshToken: true);
            if (principal == null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            // Get Email and SecurityStamp from token claims
            string userEmail = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "";
            string tokenSecurityStamp = principal.Claims.FirstOrDefault(c => c.Type == "AspNet.Identity.SecurityStamp")?.Value ?? "";

            var user = await _authRepository.GetUserByEmailAsync(userEmail);

            // Validate User & check if SecurityStamp hasn't been changed
            if (user == null || user.SecurityStamp.ToString("o") != tokenSecurityStamp)
            {
                // SecurityStamp mismatch means the token was invalidated (e.g. by password change)
                throw new UnauthorizedAccessException("Refresh token is invalid or has been revoked.");
            }

            // Issue new tokens
            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshTokenAsJwt(user);

            return new TokenModel
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _authRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                // To prevent email enumeration, we do not throw an error here but silently return.
                return;
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var cacheKey = $"RESET_OTP_{request.Email}";
            _cache.Set(cacheKey, otp, TimeSpan.FromMinutes(10));

            var htmlMessage = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Đặt lại mật khẩu</h2>
                    <p>Chào bạn,</p>
                    <p>Mã OTP để đặt lại mật khẩu của bạn là:</p>
                    <h1 style='color: #2b6cb0; letter-spacing: 5px;'>{otp}</h1>
                    <p>Mã này chỉ được sử dụng một lần và sẽ hết hạn sau 10 phút. Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này.</p>
                </div>";

            await _emailService.SendEmailAsync(request.Email, "Mã Xác Thực Đặt Lại Mật Khẩu - Math Test Creator", htmlMessage);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var cacheKey = $"RESET_OTP_{request.Email}";

            if (!_cache.TryGetValue(cacheKey, out string? cachedOtp) || string.IsNullOrEmpty(cachedOtp))
            {
                throw new UnauthorizedAccessException("Mã OTP đã hết hạn hoặc không tồn tại.");
            }

            if (cachedOtp != request.OtpCode)
            {
                throw new UnauthorizedAccessException("Mã OTP không chính xác.");
            }

            _cache.Remove(cacheKey); // OTP is valid, remove it

            var user = await _authRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                throw new InvalidOperationException(ErrorMessages.UserNotFound);
            }

            // Update user password and invalidate previous tokens
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.SecurityStamp = DateTime.UtcNow;

            await _authRepository.UpdateUserAsync(user);
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _authRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException(ErrorMessages.UserNotFound);
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new InvalidOperationException("Tài khoản của bạn được liên kết với Google. Vui lòng thiết lập mật khẩu bằng tính năng Quên mật khẩu hoặc Đăng nhập qua Google.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Mật khẩu cũ không chính xác.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.SecurityStamp = DateTime.UtcNow;

            await _authRepository.UpdateUserAsync(user);
        }

        public async Task LogoutAsync(int userId)
        {
            var user = await _authRepository.GetUserByIdAsync(userId);
            if (user != null)
            {
                // Changing the SecurityStamp invalidates all existing refresh tokens
                user.SecurityStamp = DateTime.UtcNow;
                await _authRepository.UpdateUserAsync(user);
            }
        }

        private ClaimsPrincipal? CreatePrincipalFromExpiredToken(string? token, bool isRefreshToken = false)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? ""));
            var audience = isRefreshToken ? _configuration["Jwt:Audience"] + "_Refresh" : _configuration["Jwt:Audience"];

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = isRefreshToken // Validate expiry ONLY for refresh tokens
            };

            try 
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    throw new SecurityTokenException("Invalid token signature.");

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private string GenerateRefreshTokenAsJwt(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("AspNet.Identity.SecurityStamp", user.SecurityStamp.ToString("o")) // Embed SecurityStamp
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? ""));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            // Refresh Token lives much longer, e.g., 7 days
            var expires = DateTime.UtcNow.AddDays(7); 
            var audience = _configuration["Jwt:Audience"] + "_Refresh"; // Distinguish visually from access tokens

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: audience, 
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(string idToken)
        {
            var clientId = _configuration["Google:ClientId"];
            var settings = new GoogleJsonWebSignature.ValidationSettings();
            if (!string.IsNullOrEmpty(clientId) && clientId != "YOUR_GOOGLE_CLIENT_ID_HERE")
            {
                settings.Audience = new[] { clientId };
            }

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                if (payload == null)
                    throw new UnauthorizedAccessException(ErrorMessages.InvalidGoogleToken);

                return payload;
            }
            catch (InvalidJwtException)
            {
                throw new UnauthorizedAccessException(ErrorMessages.InvalidGoogleTokenSignature);
            }
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "User")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? ""));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(15); // Adjust access token lifetime appropriately, usually shorter when using Refresh Tokens. 

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
