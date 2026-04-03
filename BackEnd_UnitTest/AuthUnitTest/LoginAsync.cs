using Backend.Constants;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Backend.DTOs;
using Microsoft.Extensions.Configuration;
using Moq;

namespace BackEnd_UnitTest.AuthUnitTest
{
    public class AuthUnitTest
    {
        private readonly Mock<IAuthRepository> _mockAuthRepo;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly AuthService _authService;

        public AuthUnitTest()
        {
            _mockAuthRepo = new Mock<IAuthRepository>();
            _mockConfig = new Mock<IConfiguration>();
            _mockEmail = new Mock<IEmailService>();
            _mockCache = new Mock<IMemoryCache>();

            _mockConfig.Setup(c => c["Jwt:Key"]).Returns("super-secret-key-for-testing-1234567890");
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
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

        [Fact]
        public async Task LoginAsync_UTCID01_ValidEmailAndPassword_ShouldReturnLoginResponse()
        {
            // Arrange
            var plainPassword = "12345678";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            var fakeUser = new User
            {
                Email = "abc@gmail.com",
                PasswordHash = hashedPassword,
                Role = new Role { Name = "User" }
            };

            var request = new LoginRequest
            {
                Email = "abc@gmail.com",
                Password = plainPassword
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync(request.Email))
                         .ReturnsAsync(fakeUser);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.NotNull(result.RefreshToken);
            Assert.Equal("User", result.RoleName);
            Assert.Equal("abc@gmail.com", result.Email);
        }

        [Fact]
        public async Task LoginAsync_UTCID02_UserNotFound_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "notexist@gmail.com",
                Password = "somepassword"
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync(request.Email))
                         .ReturnsAsync((User?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.LoginAsync(request));

            Assert.Equal(ErrorMessages.InvalidEmailOrPassword, exception.Message);
        }

        [Fact]
        public async Task LoginAsync_UTCID03_NullPassword_GoogleAccount_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var fakeUser = new User
            {
                Email = "abc@gmail.com",
                PasswordHash = null, // Tài khoản Google không có password
                Role = new Role { Name = "User" }
            };

            var request = new LoginRequest
            {
                Email = "abc@gmail.com",
                Password = null
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync(request.Email))
                         .ReturnsAsync(fakeUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.LoginAsync(request));

            Assert.Contains("Google", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_UTCID04_WrongPassword_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var correctPassword = "12345678";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(correctPassword);

            var fakeUser = new User
            {
                Email = "abc@gmail.com",
                PasswordHash = hashedPassword,
                Role = new Role { Name = "User" }
            };

            var request = new LoginRequest
            {
                Email = "abc@gmail.com",
                Password = "sai" // Sai mật khẩu
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync(request.Email))
                         .ReturnsAsync(fakeUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.LoginAsync(request));

            Assert.Equal(ErrorMessages.InvalidEmailOrPassword, exception.Message);
        }

        [Fact]
        public async Task LoginAsync_UTCID05_WrongEmail_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "sai",
                Password = "12345678"
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync(request.Email))
                         .ReturnsAsync((User?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.LoginAsync(request));

            Assert.Equal(ErrorMessages.InvalidEmailOrPassword, exception.Message);
        }

        [Fact]
        public async Task LoginAsync_UTCID06_NullEmail_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = null,
                Password = "12345678"
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync(null))
                         .ReturnsAsync((User?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.LoginAsync(request));

            Assert.Equal(ErrorMessages.InvalidEmailOrPassword, exception.Message);
        }

        // UTCID07 - Normal: Role == null → RoleName fallback "User"
        [Fact]
        public async Task LoginAsync_UTCID07_ValidCredentials_RoleNull_ShouldDefaultRoleNameToUser()
        {
            var plainPassword = "12345678";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            var fakeUser = new User
            {
                Email = "norole@gmail.com",
                PasswordHash = hashedPassword,
                Role = null
            };

            var request = new LoginRequest
            {
                Email = "norole@gmail.com",
                Password = plainPassword
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync(request.Email))
                         .ReturnsAsync(fakeUser);

            var result = await _authService.LoginAsync(request);

            Assert.NotNull(result);
            Assert.Equal("User", result.RoleName);
            Assert.Equal("norole@gmail.com", result.Email);
        }

        // UTCID08 - Normal: Role != null nhưng Name == null → fallback "User"
        [Fact]
        public async Task LoginAsync_UTCID08_ValidCredentials_RoleNameNull_ShouldDefaultRoleNameToUser()
        {
            var plainPassword = "12345678";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            var fakeUser = new User
            {
                Email = "nullname@gmail.com",
                PasswordHash = hashedPassword,
                Role = new Role { Name = null! }
            };

            var request = new LoginRequest
            {
                Email = "nullname@gmail.com",
                Password = plainPassword
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync(request.Email))
                         .ReturnsAsync(fakeUser);

            var result = await _authService.LoginAsync(request);

            Assert.NotNull(result);
            Assert.Equal("User", result.RoleName);
        }
    }
}