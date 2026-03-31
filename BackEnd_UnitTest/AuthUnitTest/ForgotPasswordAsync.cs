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

namespace Backend_UnitTest.AuthUnitTest
{
    public class AuthForgotPasswordUnitTest
    {
        private readonly Mock<IAuthRepository> _mockAuthRepo;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly AuthService _authService;

        public AuthForgotPasswordUnitTest()
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

        // UTCID01 - Normal: Thông tin đúng + Mọi service hoạt động
        [Fact]
        public async Task ForgotPasswordAsync_UTCID01_ValidRequest_AllServicesOk_ShouldSendEmail()
        {
            // Arrange
            var fakeUser = new User { Email = "email@gmail.com" };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync(fakeUser);

            _mockEmail.Setup(e => e.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var request = new ForgotPasswordRequest { Email = "email@gmail.com" };

            // Act - Không throw là thành công
            await _authService.ForgotPasswordAsync(request);

            // Assert - Verify email được gửi đúng 1 lần
            _mockEmail.Verify(e => e.SendEmailAsync(
                "email@gmail.com",
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);

            // Verify cache được set với key đúng
            _mockCache.Verify(c => c.CreateEntry(
                It.Is<object>(k => k.ToString() == "RESET_OTP_email@gmail.com")),
                Times.Once);
        }

        // UTCID02 - Abnormal: Email đúng nhưng User không tồn tại
        [Fact]
        public async Task ForgotPasswordAsync_UTCID02_UserNotFound_ShouldReturnSilently()
        {
            // Arrange - User không tồn tại
            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync((User?)null);

            var request = new ForgotPasswordRequest { Email = "email@gmail.com" };

            // Act - Không throw là đúng (silent return để tránh email enumeration)
            await _authService.ForgotPasswordAsync(request);

            // Assert - Email service và cache không được gọi
            _mockEmail.Verify(e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);

            _mockCache.Verify(c => c.CreateEntry(
                It.IsAny<object>()), Times.Never);
        }

        // UTCID03 - Abnormal: Email không tồn tại + Email Service lỗi
        [Fact]
        public async Task ForgotPasswordAsync_UTCID03_EmailNotExist_EmailServiceFails_ShouldReturnSilently()
        {
            // Arrange - User không tồn tại với email không hợp lệ
            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("nonexist"))
                         .ReturnsAsync((User?)null);

            // Email service có lỗi nhưng sẽ không được gọi
            _mockEmail.Setup(e => e.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service error"));

            var request = new ForgotPasswordRequest { Email = "nonexist" };

            // Act - Không throw vì user null → silent return trước khi reach email service
            await _authService.ForgotPasswordAsync(request);

            // Assert - Email service không được gọi
            _mockEmail.Verify(e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        // UTCID04 - Normal: User tồn tại + Email Service hoạt động
        [Fact]
        public async Task ForgotPasswordAsync_UTCID04_UserExists_EmailServiceOk_ShouldSendEmail()
        {
            // Arrange
            var fakeUser = new User { Email = "email@gmail.com" };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync(fakeUser);

            _mockEmail.Setup(e => e.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var request = new ForgotPasswordRequest { Email = "email@gmail.com" };

            // Act
            await _authService.ForgotPasswordAsync(request);

            // Assert
            _mockEmail.Verify(e => e.SendEmailAsync(
                "email@gmail.com",
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        // UTCID05 - Normal: User tồn tại nhưng Email Service ngắt kết nối
        [Fact]
        public async Task ForgotPasswordAsync_UTCID05_UserExists_EmailServiceDisconnected_ShouldThrowException()
        {
            // Arrange
            var fakeUser = new User { Email = "email@gmail.com" };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync(fakeUser);

            // Email service ngắt kết nối → throw exception
            _mockEmail.Setup(e => e.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP connection lost"));

            var request = new ForgotPasswordRequest { Email = "email@gmail.com" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _authService.ForgotPasswordAsync(request));

            Assert.Equal("SMTP connection lost", exception.Message);

            // Cache đã được set trước khi email lỗi
            _mockCache.Verify(c => c.CreateEntry(
                It.Is<object>(k => k.ToString() == "RESET_OTP_email@gmail.com")),
                Times.Once);
        }
    }
}
