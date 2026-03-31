using Backend.DTOs.Auth;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Backend_UnitTest.AuthUnitTest
{
    public class AuthRefreshTokenUnitTest
    {
        private readonly Mock<IAuthRepository> _mockAuthRepo;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly AuthService _authService;

        private const string JwtKey = "super-secret-key-for-testing-1234567890";
        private const string JwtIssuer = "TestIssuer";
        private const string JwtAudience = "TestAudience";

        public AuthRefreshTokenUnitTest()
        {
            _mockAuthRepo = new Mock<IAuthRepository>();
            _mockConfig = new Mock<IConfiguration>();
            _mockEmail = new Mock<IEmailService>();
            _mockCache = new Mock<IMemoryCache>();

            _mockConfig.Setup(c => c["Jwt:Key"]).Returns(JwtKey);
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns(JwtIssuer);
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns(JwtAudience);
            _mockConfig.Setup(c => c["Jwt:ExpireMinutes"]).Returns("60");

            object cacheValue = null;
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
                      .Returns(false);

            _authService = new AuthService(
                _mockAuthRepo.Object,
                _mockConfig.Object,
                _mockEmail.Object,
                _mockCache.Object
            );
        }

        private string GenerateValidRefreshToken(string email, string securityStamp)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim("AspNet.Identity.SecurityStamp", securityStamp)
            };
            var token = new JwtSecurityToken(
                issuer: JwtIssuer,
                audience: JwtAudience + "_Refresh", // isRefreshToken = true
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // UTCID01 - Normal: Mọi dữ liệu hợp lệ → Thành công
        [Fact]
        public async Task RefreshTokenAsync_UTCID01_ValidRequest_ShouldReturnNewTokens()
        {
            // Arrange
            var securityStamp = DateTime.UtcNow;
            var fakeUser = new User
            {
                Email = "email@gmail.com",
                SecurityStamp = securityStamp,
                Role = new Role { Name = "User" }
            };

            var refreshToken = GenerateValidRefreshToken(
                "email@gmail.com",
                securityStamp.ToString("o")
            );

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync(fakeUser);

            var request = new TokenModel { RefreshToken = refreshToken };

            // Act
            var result = await _authService.RefreshTokenAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
            Assert.NotEqual(refreshToken, result.RefreshToken); // Token mới khác token cũ
        }

        // UTCID02 - Normal: Request bị null → ArgumentNullException
        [Fact]
        public async Task RefreshTokenAsync_UTCID02_NullRequest_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _authService.RefreshTokenAsync(null));
        }

        // UTCID03 - Normal: RefreshToken sai định dạng → ArgumentNullException
        [Fact]
        public async Task RefreshTokenAsync_UTCID03_InvalidFormatToken_ShouldThrowArgumentNullException()
        {
            // Arrange - RefreshToken rỗng/null
            var request = new TokenModel { RefreshToken = null };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _authService.RefreshTokenAsync(request));
        }

        // UTCID04 - Normal: Request đúng nhưng Token không parse được
        [Fact]
        public async Task RefreshTokenAsync_UTCID04_TokenCannotBeParsed_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange - Token có cấu trúc nhưng sai key/signature → parse trả về null
            var request = new TokenModel
            {
                RefreshToken = "invalid.token.string"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.RefreshTokenAsync(request));

            Assert.Equal("Invalid refresh token.", exception.Message);
        }

        // UTCID05 - Normal: Token parse được nhưng SecurityStamp không khớp
        [Fact]
        public async Task RefreshTokenAsync_UTCID05_SecurityStampMismatch_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange - Token có SecurityStamp cũ, user đã đổi password
            var oldSecurityStamp = DateTime.UtcNow.AddHours(-1); // stamp cũ trong token
            var newSecurityStamp = DateTime.UtcNow;              // stamp mới trong DB

            var fakeUser = new User
            {
                Email = "email@gmail.com",
                SecurityStamp = newSecurityStamp, // DB có stamp mới
                Role = new Role { Name = "User" }
            };

            var refreshToken = GenerateValidRefreshToken(
                "email@gmail.com",
                oldSecurityStamp.ToString("o") // Token chứa stamp cũ
            );

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync(fakeUser);

            var request = new TokenModel { RefreshToken = refreshToken };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.RefreshTokenAsync(request));

            Assert.Equal("Refresh token is invalid or has been revoked.", exception.Message);
        }

        // UTCID06 - Normal: Mọi thứ OK nhưng User không tồn tại
        [Fact]
        public async Task RefreshTokenAsync_UTCID06_UserNotFound_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange - Token hợp lệ nhưng user không còn trong DB
            var securityStamp = DateTime.UtcNow;
            var refreshToken = GenerateValidRefreshToken(
                "email@gmail.com",
                securityStamp.ToString("o")
            );

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync((User?)null); // User không tồn tại

            var request = new TokenModel { RefreshToken = refreshToken };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.RefreshTokenAsync(request));

            Assert.Equal("Refresh token is invalid or has been revoked.", exception.Message);
        }
    }
}
