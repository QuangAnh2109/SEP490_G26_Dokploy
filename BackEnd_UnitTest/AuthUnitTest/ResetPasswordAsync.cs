using Backend.Constants;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;  
using Backend.DTOs.Auth;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackEnd_UnitTest.AuthUnitTest
{
    public class AuthResetPasswordUnitTest
    {
        private readonly Mock<IAuthRepository> _mockAuthRepo;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly AuthService _authService;

        public AuthResetPasswordUnitTest()
        {
            _mockAuthRepo = new Mock<IAuthRepository>();
            _mockConfig = new Mock<IConfiguration>();
            _mockEmail = new Mock<IEmailService>();
            _mockCache = new Mock<IMemoryCache>();

            _mockConfig.Setup(c => c["Jwt:Key"]).Returns("super-secret-key-for-testing-1234567890");
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
            _mockConfig.Setup(c => c["Jwt:ExpireMinutes"]).Returns("60");

            // Mock CreateEntry cho _cache.Set()
            var mockCacheEntry = new Mock<ICacheEntry>();
            mockCacheEntry.SetupAllProperties();
            _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>()))
                      .Returns(mockCacheEntry.Object);

            // Mock Remove
            _mockCache.Setup(c => c.Remove(It.IsAny<object>()));

            _authService = new AuthService(
                _mockAuthRepo.Object,
                _mockConfig.Object,
                _mockEmail.Object,
                _mockCache.Object
            );
        }

        private void SetupValidOtpCache(string email, string otp)
        {
            object cachedOtp = otp;
            _mockCache.Setup(c => c.TryGetValue(
                    It.Is<object>(k => k.ToString() == $"RESET_OTP_{email}"),
                    out cachedOtp))
                .Returns(true);
        }

        private void SetupEmptyOtpCache(string email)
        {
            object cachedOtp = null;
            _mockCache.Setup(c => c.TryGetValue(
                    It.Is<object>(k => k.ToString() == $"RESET_OTP_{email}"),
                    out cachedOtp))
                .Returns(false);
        }

        // UTCID01 - Normal: Mọi dữ liệu đều đúng → Đổi mật khẩu thành công
        [Fact]
        public async Task ResetPasswordAsync_UTCID01_ValidOtp_ValidPassword_ShouldResetPasswordSuccessfully()
        {
            // Arrange
            SetupValidOtpCache("email@gmail.com", "123456");

            var fakeUser = new User
            {
                Email = "email@gmail.com",
                PasswordHash = "oldHashedPassword",
                SecurityStamp = DateTime.UtcNow.AddDays(-1)
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync(fakeUser);

            _mockAuthRepo.Setup(r => r.AddUserAsync(It.IsAny<User>()))
                         .ReturnsAsync((User u) => u);

            var request = new ResetPasswordRequest
            {
                Email = "email@gmail.com",
                OtpCode = "123456",
                NewPassword = "newPassword123"
            };

            // Act - Không throw là thành công
            await _authService.ResetPasswordAsync(request);

            // Assert - Verify UpdateUserAsync được gọi với password mới
            _mockAuthRepo.Verify(r => r.UpdateUserAsync(
                It.Is<User>(u => u.Email == "email@gmail.com")),
                Times.Once);
            Assert.True(BCrypt.Net.BCrypt.Verify("newPassword123", fakeUser.PasswordHash));

            // Verify OTP bị xóa khỏi cache
            _mockCache.Verify(c => c.Remove(
                It.Is<object>(k => k.ToString() == "RESET_OTP_email@gmail.com")),
                Times.Once);
        }

        // UTCID02 - Abnormal: OTP nhập sai
        [Fact]
        public async Task ResetPasswordAsync_UTCID02_WrongOtp_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange - Cache có OTP "123456" nhưng request gửi OTP sai
            SetupValidOtpCache("email@gmail.com", "123456");

            var request = new ResetPasswordRequest
            {
                Email = "email@gmail.com",
                OtpCode = "999999", // OTP sai
                NewPassword = "newPassword123"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.ResetPasswordAsync(request));

            Assert.Equal("Mã OTP không chính xác.", exception.Message);

            // UpdateUserAsync không được gọi
            _mockAuthRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        }

        // UTCID03 - Abnormal: OTP hết hạn hoặc không tồn tại trong Cache
        [Fact]
        public async Task ResetPasswordAsync_UTCID03_OtpExpiredOrNotExists_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange - Cache không có OTP
            SetupEmptyOtpCache("email@gmail.com");

            var request = new ResetPasswordRequest
            {
                Email = "email@gmail.com",
                OtpCode = "123456",
                NewPassword = "newPassword123"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.ResetPasswordAsync(request));

            Assert.Equal("Mã OTP đã hết hạn hoặc không tồn tại.", exception.Message);

            // UpdateUserAsync không được gọi
            _mockAuthRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        }

        // UTCID04 - Normal: OTP đúng nhưng User không tồn tại
        [Fact]
        public async Task ResetPasswordAsync_UTCID04_ValidOtp_UserNotFound_ShouldThrowInvalidOperationException()
        {
            // Arrange
            SetupValidOtpCache("email@gmail.com", "123456");

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync((User?)null);

            var request = new ResetPasswordRequest
            {
                Email = "email@gmail.com",
                OtpCode = "123456",
                NewPassword = "newPassword123"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.ResetPasswordAsync(request));

            Assert.Equal(ErrorMessages.UserNotFound, exception.Message);

            // UpdateUserAsync không được gọi
            _mockAuthRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        }

        // UTCID05 - Abnormal: OTP đúng nhưng mật khẩu mới không hợp lệ
        [Fact]
        public async Task ResetPasswordAsync_UTCID05_ValidOtp_InvalidNewPassword_ShouldStillComplete()
        {
            // Arrange
            SetupValidOtpCache("email@gmail.com", "123456");

            var fakeUser = new User
            {
                Email = "email@gmail.com",
                PasswordHash = "oldHashedPassword",
                SecurityStamp = DateTime.UtcNow.AddDays(-1)
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync(fakeUser);

            _mockAuthRepo.Setup(r => r.AddUserAsync(It.IsAny<User>()))
             .ReturnsAsync((User u) => u);

            var request = new ResetPasswordRequest
            {
                Email = "email@gmail.com",
                OtpCode = "123456",
                NewPassword = "" // Mật khẩu không hợp lệ (rỗng)
            };

            // Act - Source code không validate NewPassword nên vẫn chạy qua
            await _authService.ResetPasswordAsync(request);

            // Assert - UpdateUserAsync vẫn được gọi
            _mockAuthRepo.Verify(r => r.UpdateUserAsync(
                It.IsAny<User>()), Times.Once);
        }
    }
}
