using Backend.Constants;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BackEnd_UnitTest.AuthUnitTest
{
    public class AuthGoogleRegisterUnitTest
    {
        private readonly Mock<IAuthRepository> _mockAuthRepo;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IMemoryCache> _mockCache;

        public AuthGoogleRegisterUnitTest()
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

        // UTCID01 - Normal: Token đúng + Role đúng → Đăng ký thành công
        [Fact]
        public async Task GoogleRegisterAsync_UTCID01_ValidToken_ValidRole_ShouldReturnLoginResponse()
        {
            // Arrange
            var fakePayload = new GoogleJsonWebSignature.Payload
            {
                Email = "email@gmail.com"
            };

            // User chưa tồn tại
            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync((User?)null);

            // AddUserAsync không làm gì cả (void)
            _mockAuthRepo.Setup(r => r.AddUserAsync(It.IsAny<User>()))
                         .ReturnsAsync((User u) => u);

            var service = CreateService(fakePayload: fakePayload);
            var request = new GoogleRegisterRequest
            {
                IdToken = "valid-token",
                RoleId = 1 // RoleId hợp lệ
            };

            // Act
            var result = await service.GoogleRegisterAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.NotNull(result.RefreshToken);
            Assert.Equal("email@gmail.com", result.Email);
            // Role chưa được load từ DB nên fallback về "User"
            Assert.Equal("User", result.RoleName);
        }

        // UTCID02 - Abnormal: Token đúng + User đã tồn tại
        [Fact]
        public async Task GoogleRegisterAsync_UTCID02_ValidToken_UserAlreadyExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var fakePayload = new GoogleJsonWebSignature.Payload
            {
                Email = "email@gmail.com"
            };

            var existingUser = new User
            {
                Email = "email@gmail.com",
                PasswordHash = null,
                Role = new Role { Name = "User" }
            };

            // User đã tồn tại trong hệ thống
            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync(existingUser);

            var service = CreateService(fakePayload: fakePayload);
            var request = new GoogleRegisterRequest
            {
                IdToken = "valid-token",
                RoleId = 1
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GoogleRegisterAsync(request));

            Assert.Equal(ErrorMessages.UserAlreadyExists, exception.Message);
        }

        // UTCID03 - Normal: Token Google sai → UnauthorizedAccessException
        [Fact]
        public async Task GoogleRegisterAsync_UTCID03_InvalidToken_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange - Token sai nên throw ngay, không reach tới check user
            var service = CreateService(shouldThrow: true);
            var request = new GoogleRegisterRequest
            {
                IdToken = "invalid-token",
                RoleId = 1
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GoogleRegisterAsync(request));

            Assert.Equal(ErrorMessages.InvalidGoogleTokenSignature, exception.Message);
        }

        // UTCID04 - Abnormal: Token đúng + RoleId sai
        [Fact]
        public async Task GoogleRegisterAsync_UTCID04_ValidToken_InvalidRoleId_ShouldReturnDefaultRoleName()
        {
            // Arrange
            var fakePayload = new GoogleJsonWebSignature.Payload
            {
                Email = "email@gmail.com"
            };

            // User chưa tồn tại
            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync((User?)null);

            _mockAuthRepo.Setup(r => r.AddUserAsync(It.IsAny<User>()))
                         .ReturnsAsync((User u) => u);

            var service = CreateService(fakePayload: fakePayload);
            var request = new GoogleRegisterRequest
            {
                IdToken = "valid-token",
                RoleId = 999 // RoleId không tồn tại trong DB
            };

            // Act
            var result = await service.GoogleRegisterAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.NotNull(result.RefreshToken);
            Assert.Equal("email@gmail.com", result.Email);
            // RoleId sai → Role không load được → fallback "User"
            Assert.Equal("User", result.RoleName);
        }

        // UTCID06 - AddUser trả về user có Role.Name cụ thể → RoleName từ Role, không fallback "User"
        [Fact]
        public async Task GoogleRegisterAsync_UTCID06_ValidToken_UserWithRoleName_ShouldReturnRoleNameFromRole()
        {
            var fakePayload = new GoogleJsonWebSignature.Payload
            {
                Email = "roleuser@gmail.com"
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("roleuser@gmail.com"))
                         .ReturnsAsync((User?)null);

            _mockAuthRepo.Setup(r => r.AddUserAsync(It.IsAny<User>()))
                         .ReturnsAsync((User u) =>
                         {
                             u.Role = new Role { Name = "Teacher" };
                             return u;
                         });

            var service = CreateService(fakePayload: fakePayload);
            var request = new GoogleRegisterRequest
            {
                IdToken = "valid-token",
                RoleId = 1
            };

            var result = await service.GoogleRegisterAsync(request);

            Assert.NotNull(result);
            Assert.Equal("Teacher", result.RoleName);
            Assert.Equal("roleuser@gmail.com", result.Email);
        }
    }
}
