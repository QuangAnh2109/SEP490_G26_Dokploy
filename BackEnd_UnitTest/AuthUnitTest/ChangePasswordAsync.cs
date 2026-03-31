using Backend.Constants;
using Backend.DTOs.Auth;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackEnd_UnitTest.AuthUnitTest
{
    public class AuthChangePasswordUnitTest
    {
        private readonly Mock<IAuthRepository> _mockAuthRepo;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly AuthService _authService;

        public AuthChangePasswordUnitTest()
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

        // UTCID01 - Normal: Mọi thông tin đều chính xác → Thành công
        [Fact]
        public async Task ChangePasswordAsync_UTCID01_ValidRequest_ShouldChangePasswordSuccessfully()
        {
            // Arrange
            var oldPassword = "oldPassword123";
            var fakeUser = new User
            {
                UserId = 1,
                Email = "email@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword),
                SecurityStamp = DateTime.UtcNow.AddDays(-1)
            };

            _mockAuthRepo.Setup(r => r.GetUserByIdAsync(1))
                         .ReturnsAsync(fakeUser);

            _mockAuthRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>()))
             .ReturnsAsync((User u) => u);

            var request = new ChangePasswordRequest
            {
                OldPassword = oldPassword,
                NewPassword = "newPassword123"
            };

            // Act
            await _authService.ChangePasswordAsync(1, request);

            // Assert - UpdateUserAsync được gọi đúng 1 lần
            _mockAuthRepo.Verify(r => r.UpdateUserAsync(
                It.Is<User>(u => u.UserId == 1)),
                Times.Once);

            // Verify password đã được hash và update
            Assert.True(BCrypt.Net.BCrypt.Verify("newPassword123", fakeUser.PasswordHash));
        }

        // UTCID02 - Normal: User không tồn tại
        //           → InvalidOperationException (UserNotFound)
        [Fact]
        public async Task ChangePasswordAsync_UTCID02_UserNotFound_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mockAuthRepo.Setup(r => r.GetUserByIdAsync(999))
                         .ReturnsAsync((User?)null);

            var request = new ChangePasswordRequest
            {
                OldPassword = "oldPassword123",
                NewPassword = "newPassword123"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.ChangePasswordAsync(999, request));

            Assert.Equal(ErrorMessages.UserNotFound, exception.Message);

            // UpdateUserAsync không được gọi
            _mockAuthRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        }

        // UTCID03 - Abnormal: Nhập sai mật khẩu cũ
        //           → UnauthorizedAccessException
        [Fact]
        public async Task ChangePasswordAsync_UTCID03_WrongOldPassword_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var fakeUser = new User
            {
                UserId = 1,
                Email = "email@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctPassword123")
            };

            _mockAuthRepo.Setup(r => r.GetUserByIdAsync(1))
                         .ReturnsAsync(fakeUser);

            var request = new ChangePasswordRequest
            {
                OldPassword = "wrongPassword", // Sai mật khẩu cũ
                NewPassword = "newPassword123"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.ChangePasswordAsync(1, request));

            Assert.Equal("Mật khẩu cũ không chính xác.", exception.Message);

            // UpdateUserAsync không được gọi
            _mockAuthRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        }

        // UTCID04 - Abnormal: Tài khoản Google (PasswordHash null)
        //           → InvalidOperationException
        [Fact]
        public async Task ChangePasswordAsync_UTCID04_GoogleAccount_ShouldThrowInvalidOperationException()
        {
            // Arrange - Tài khoản Google không có PasswordHash
            var fakeUser = new User
            {
                UserId = 1,
                Email = "email@gmail.com",
                PasswordHash = null // Tài khoản Google
            };

            _mockAuthRepo.Setup(r => r.GetUserByIdAsync(1))
                         .ReturnsAsync(fakeUser);

            var request = new ChangePasswordRequest
            {
                OldPassword = "oldPassword123",
                NewPassword = "invalidNewPass!"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.ChangePasswordAsync(1, request));

            Assert.Contains("Google", exception.Message);

            // UpdateUserAsync không được gọi
            _mockAuthRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        }

        // UTCID05 - Abnormal: Mật khẩu mới bị null
        //           → Source code không validate NewPassword null
        //             → BCrypt.HashPassword(null) sẽ throw Exception
        [Fact]
        public async Task ChangePasswordAsync_UTCID05_NullNewPassword_ShouldThrowException()
        {
            // Arrange
            var oldPassword = "oldPassword123";
            var fakeUser = new User
            {
                UserId = 1,
                Email = "email@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword)
            };

            _mockAuthRepo.Setup(r => r.GetUserByIdAsync(1))
                         .ReturnsAsync(fakeUser);

            var request = new ChangePasswordRequest
            {
                OldPassword = oldPassword,
                NewPassword = null // Mật khẩu mới null
            };

            // Act & Assert - BCrypt.HashPassword(null) sẽ throw
            await Assert.ThrowsAnyAsync<Exception>(() =>
                _authService.ChangePasswordAsync(1, request));

            // UpdateUserAsync không được gọi
            _mockAuthRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        }
    }
}
