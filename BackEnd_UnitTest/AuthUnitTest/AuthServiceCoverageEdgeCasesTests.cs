using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace BackEnd_UnitTest.AuthUnitTest
{
    /// <summary>
    /// Nhánh coverage khó đạt qua reflection + JWT tùy chỉnh (chỉ sửa test project).
    /// </summary>
    public class AuthServiceCoverageEdgeCasesTests
    {
        private const string JwtKey = "super-secret-key-for-testing-1234567890";
        private const string JwtIssuer = "TestIssuer";
        private const string JwtAudience = "TestAudience";

        /// <summary>HS512 cần key ≥ 64 byte; dùng chung cho ký token và validate trong test.</summary>
        private static readonly string Hs512SigningKey = new string('k', 64);

        /// <summary>
        /// Google ClientId = placeholder → không gán Audience; JWT không hợp lệ → UnauthorizedAccessException (InvalidJwtException bọc).
        /// </summary>
        [Fact]
        public async Task ValidateGoogleTokenAsync_InvalidJwt_ClientIdPlaceholder_SkipsAudienceBranch()
        {
            var mockRepo = new Mock<IAuthRepository>();
            var mockConfig = new Mock<IConfiguration>();
            var mockEmail = new Mock<IEmailService>();
            var mockCache = new Mock<IMemoryCache>();

            mockConfig.Setup(c => c["Google:ClientId"]).Returns("YOUR_GOOGLE_CLIENT_ID_HERE");
            mockConfig.Setup(c => c["Jwt:Key"]).Returns(JwtKey);
            mockConfig.Setup(c => c["Jwt:Issuer"]).Returns(JwtIssuer);
            mockConfig.Setup(c => c["Jwt:Audience"]).Returns(JwtAudience);

            object cv = null;
            mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cv)).Returns(false);

            var service = new AuthService(mockRepo.Object, mockConfig.Object, mockEmail.Object, mockCache.Object);

            var method = typeof(AuthService).GetMethod(
                "ValidateGoogleTokenAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var task = (Task<GoogleJsonWebSignature.Payload>)method!.Invoke(service, new object[] { "not-a-real-jwt" })!;
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await task);
        }

        /// <summary>
        /// Google ClientId thật → có Audience; JWT rác → lỗi từ Google validate (khác nhánh placeholder ở trên).
        /// </summary>
        [Fact]
        public async Task ValidateGoogleTokenAsync_InvalidJwt_ThrowsUnauthorized_AudienceConfigured()
        {
            var mockRepo = new Mock<IAuthRepository>();
            var mockConfig = new Mock<IConfiguration>();
            var mockEmail = new Mock<IEmailService>();
            var mockCache = new Mock<IMemoryCache>();

            mockConfig.Setup(c => c["Google:ClientId"]).Returns("real_google_client_id");
            mockConfig.Setup(c => c["Jwt:Key"]).Returns(JwtKey);
            mockConfig.Setup(c => c["Jwt:Issuer"]).Returns(JwtIssuer);
            mockConfig.Setup(c => c["Jwt:Audience"]).Returns(JwtAudience);

            object cv = null;
            mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cv)).Returns(false);

            var service = new AuthService(mockRepo.Object, mockConfig.Object, mockEmail.Object, mockCache.Object);

            var method = typeof(AuthService).GetMethod(
                "ValidateGoogleTokenAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var task = (Task<GoogleJsonWebSignature.Payload>)method!.Invoke(service, new object[] { "not-a-real-jwt" })!;
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await task);
        }

        /// <summary>
        /// Token HS512 validate OK nhưng khác HmacSha256 → throw SecurityTokenException rồi catch trả null.
        /// </summary>
        [Fact]
        public void CreatePrincipalFromExpiredToken_Hs512AfterValidation_ReturnsNull()
        {
            var mockRepo = new Mock<IAuthRepository>();
            var mockConfig = new Mock<IConfiguration>();
            var mockEmail = new Mock<IEmailService>();
            var mockCache = new Mock<IMemoryCache>();

            mockConfig.Setup(c => c["Jwt:Key"]).Returns(Hs512SigningKey);
            mockConfig.Setup(c => c["Jwt:Issuer"]).Returns(JwtIssuer);
            mockConfig.Setup(c => c["Jwt:Audience"]).Returns(JwtAudience);

            var service = new AuthService(mockRepo.Object, mockConfig.Object, mockEmail.Object, mockCache.Object);

            var token = BuildRefreshTokenHs512(Hs512SigningKey);

            var method = typeof(AuthService).GetMethod(
                "CreatePrincipalFromExpiredToken",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var result = method!.Invoke(service, new object[] { token, true });
            Assert.Null(result);
        }

        /// <summary>
        /// Access token HS256, isRefreshToken=false → validate OK, audience issuer thường → trả ClaimsPrincipal.
        /// </summary>
        [Fact]
        public void CreatePrincipalFromExpiredToken_AccessToken_IsRefreshTokenFalse_ReturnsPrincipal()
        {
            var mockRepo = new Mock<IAuthRepository>();
            var mockConfig = new Mock<IConfiguration>();
            var mockEmail = new Mock<IEmailService>();
            var mockCache = new Mock<IMemoryCache>();

            mockConfig.Setup(c => c["Jwt:Key"]).Returns(JwtKey);
            mockConfig.Setup(c => c["Jwt:Issuer"]).Returns(JwtIssuer);
            mockConfig.Setup(c => c["Jwt:Audience"]).Returns(JwtAudience);

            var service = new AuthService(mockRepo.Object, mockConfig.Object, mockEmail.Object, mockCache.Object);

            var token = BuildAccessTokenHs256();

            var method = typeof(AuthService).GetMethod(
                "CreatePrincipalFromExpiredToken",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var principal = method!.Invoke(service, new object[] { token, false }) as ClaimsPrincipal;
            Assert.NotNull(principal);
        }

        /// <summary>
        /// Covers <c>_configuration["Jwt:Key"] ?? ""</c> when key is missing (coalesce to empty string; SymmetricSecurityKey then throws).
        /// </summary>
        [Fact]
        public void CreatePrincipalFromExpiredToken_JwtKeyNull_ThrowsFromSymmetricSecurityKey()
        {
            var mockRepo = new Mock<IAuthRepository>();
            var mockConfig = new Mock<IConfiguration>();
            var mockEmail = new Mock<IEmailService>();
            var mockCache = new Mock<IMemoryCache>();

            mockConfig.Setup(c => c["Jwt:Key"]).Returns((string)null!);
            mockConfig.Setup(c => c["Jwt:Issuer"]).Returns(JwtIssuer);
            mockConfig.Setup(c => c["Jwt:Audience"]).Returns(JwtAudience);

            var service = new AuthService(mockRepo.Object, mockConfig.Object, mockEmail.Object, mockCache.Object);

            var method = typeof(AuthService).GetMethod(
                "CreatePrincipalFromExpiredToken",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var ex = Assert.Throws<TargetInvocationException>(() =>
                method!.Invoke(service, new object[] { "not-used", true }));
            Assert.NotNull(ex.InnerException);
        }

        /// <summary>
        /// Jwt:Key null → nhánh <c>?? ""</c> rồi SymmetricSecurityKey ném lỗi khi tạo token access.
        /// </summary>
        [Fact]
        public void GenerateJwtToken_JwtKeyNull_ThrowsFromSymmetricSecurityKey()
        {
            var mockRepo = new Mock<IAuthRepository>();
            var mockConfig = new Mock<IConfiguration>();
            var mockEmail = new Mock<IEmailService>();
            var mockCache = new Mock<IMemoryCache>();

            mockConfig.Setup(c => c["Jwt:Key"]).Returns((string)null!);
            mockConfig.Setup(c => c["Jwt:Issuer"]).Returns(JwtIssuer);
            mockConfig.Setup(c => c["Jwt:Audience"]).Returns(JwtAudience);

            var service = new AuthService(mockRepo.Object, mockConfig.Object, mockEmail.Object, mockCache.Object);

            var method = typeof(AuthService).GetMethod(
                "GenerateJwtToken",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var user = new User
            {
                UserId = 1,
                Email = "a@test.com",
                SecurityStamp = DateTime.UtcNow,
                PasswordHash = "x",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var ex = Assert.Throws<TargetInvocationException>(() =>
                method!.Invoke(service, new object[] { user }));
            Assert.NotNull(ex.InnerException);
        }

        /// <summary>
        /// Jwt:Key null → nhánh <c>?? ""</c> rồi SymmetricSecurityKey ném lỗi khi tạo refresh JWT.
        /// </summary>
        [Fact]
        public void GenerateRefreshTokenAsJwt_JwtKeyNull_ThrowsFromSymmetricSecurityKey()
        {
            var mockRepo = new Mock<IAuthRepository>();
            var mockConfig = new Mock<IConfiguration>();
            var mockEmail = new Mock<IEmailService>();
            var mockCache = new Mock<IMemoryCache>();

            mockConfig.Setup(c => c["Jwt:Key"]).Returns((string)null!);
            mockConfig.Setup(c => c["Jwt:Issuer"]).Returns(JwtIssuer);
            mockConfig.Setup(c => c["Jwt:Audience"]).Returns(JwtAudience);

            var service = new AuthService(mockRepo.Object, mockConfig.Object, mockEmail.Object, mockCache.Object);

            var method = typeof(AuthService).GetMethod(
                "GenerateRefreshTokenAsJwt",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var user = new User
            {
                UserId = 1,
                Email = "a@test.com",
                SecurityStamp = DateTime.UtcNow,
                PasswordHash = "x",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var ex = Assert.Throws<TargetInvocationException>(() =>
                method!.Invoke(service, new object[] { user }));
            Assert.NotNull(ex.InnerException);
        }

        private static string BuildRefreshTokenHs512(string signingKey)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, "e@test.com"),
                new Claim("AspNet.Identity.SecurityStamp", DateTime.UtcNow.ToString("o"))
            };
            var jwt = new JwtSecurityToken(
                issuer: JwtIssuer,
                audience: JwtAudience + "_Refresh",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        private static string BuildAccessTokenHs256()
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, "e@test.com"),
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var jwt = new JwtSecurityToken(
                issuer: JwtIssuer,
                audience: JwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
