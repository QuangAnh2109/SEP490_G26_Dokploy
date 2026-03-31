using Backend.Constants;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

using Moq;

namespace BackEnd_UnitTest.AuthUnitTest
{
    public class TestableAuthService : AuthService
    {
        private readonly GoogleJsonWebSignature.Payload _fakePayload;
        private readonly bool _shouldThrow;

        public TestableAuthService(
            IAuthRepository authRepository,
            IConfiguration configuration,
            IEmailService emailService,
            IMemoryCache cache,
            GoogleJsonWebSignature.Payload fakePayload = null,
            bool shouldThrow = false)
            : base(authRepository, configuration, emailService, cache)
        {
            _fakePayload = fakePayload;
            _shouldThrow = shouldThrow;
        }

        protected override async Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(string idToken)
        {
            if (_shouldThrow)
                throw new UnauthorizedAccessException(ErrorMessages.InvalidGoogleTokenSignature);

            return await Task.FromResult(_fakePayload);
        }
    }

    public class AuthGoogleUnitTest
    {
        private readonly Mock<IAuthRepository> _mockAuthRepo;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IMemoryCache> _mockCache;

        public AuthGoogleUnitTest()
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
        }

        private TestableAuthService CreateService(
            GoogleJsonWebSignature.Payload fakePayload = null,
            bool shouldThrow = false)
        {
            return new TestableAuthService(
                _mockAuthRepo.Object,
                _mockConfig.Object,
                _mockEmail.Object,
                _mockCache.Object,
                fakePayload,
                shouldThrow
            );
        }

        [Fact]
        public async Task GoogleLoginAsync_UTCID01_ValidToken_ExistingUser_ShouldReturnLoginResponse()
        {
            // Arrange
            var fakePayload = new GoogleJsonWebSignature.Payload
            {
                Email = "abc@gmail.com"
            };

            var fakeUser = new User
            {
                Email = "abc@gmail.com",
                PasswordHash = "someHash",
                Role = new Role { Name = "User" }
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("abc@gmail.com"))
                         .ReturnsAsync(fakeUser);

            var service = CreateService(fakePayload: fakePayload);
            var request = new GoogleLoginRequest { IdToken = "valid-token" };

            // Act
            var result = await service.GoogleLoginAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.NeedsRegistration);
            Assert.NotNull(result.Token);
            Assert.NotNull(result.RefreshToken);
            Assert.Equal("abc@gmail.com", result.Email);
            Assert.Equal("User", result.RoleName);
        }

        [Fact]
        public async Task GoogleLoginAsync_UTCID02_ValidToken_UserNotFound_ShouldReturnNeedsRegistration()
        {
            // Arrange
            var fakePayload = new GoogleJsonWebSignature.Payload
            {
                Email = "newuser@gmail.com"
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("newuser@gmail.com"))
                         .ReturnsAsync((User?)null);

            var service = CreateService(fakePayload: fakePayload);
            var request = new GoogleLoginRequest { IdToken = "valid-token" };

            // Act
            var result = await service.GoogleLoginAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.NeedsRegistration);
            Assert.Equal("newuser@gmail.com", result.Email);
            Assert.True(string.IsNullOrEmpty(result.Token));
        }

        [Fact]
        public async Task GoogleLoginAsync_UTCID03_InvalidToken_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var service = CreateService(shouldThrow: true);
            var request = new GoogleLoginRequest { IdToken = "invalid-token" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GoogleLoginAsync(request));

            Assert.Equal(ErrorMessages.InvalidGoogleTokenSignature, exception.Message);
        }

        [Fact]
        public async Task GoogleLoginAsync_UTCID04_ValidToken_UserWithNullRole_ShouldDefaultRoleNameToUser()
        {
            // Arrange
            var fakePayload = new GoogleJsonWebSignature.Payload
            {
                Email = "abc@gmail.com"
            };

            var fakeUser = new User
            {
                Email = "abc@gmail.com",
                PasswordHash = "someHash",
                Role = null // Role bị null → fallback về "User"
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("abc@gmail.com"))
                         .ReturnsAsync(fakeUser);

            var service = CreateService(fakePayload: fakePayload);
            var request = new GoogleLoginRequest { IdToken = "valid-token" };

            // Act
            var result = await service.GoogleLoginAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("User", result.RoleName); // fallback "User" khi Role null
            Assert.Equal("abc@gmail.com", result.Email);
            Assert.NotNull(result.Token);
            Assert.False(result.NeedsRegistration);
        }

        [Fact]
        public async Task GoogleLoginAsync_UTCID05_InvalidToken_UserWithNullRole_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange - Token sai nên không bao giờ reach tới phần check user
            var service = CreateService(shouldThrow: true);
            var request = new GoogleLoginRequest { IdToken = "invalid-token" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GoogleLoginAsync(request));

            Assert.Equal(ErrorMessages.InvalidGoogleTokenSignature, exception.Message);
        }
    }
}