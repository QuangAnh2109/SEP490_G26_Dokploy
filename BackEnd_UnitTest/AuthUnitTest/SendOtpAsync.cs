using Backend.Constants;
using Backend.Models;
using Backend.DTOs;
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

namespace Backend_UnitTest.AuthUnitTest
{
    public class AuthSendOtpUnitTest
    {
        private readonly Mock<IAuthRepository> _mockAuthRepo;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly AuthService _authService;

        public AuthSendOtpUnitTest()
        {
            _mockAuthRepo = new Mock<IAuthRepository>();
            _mockConfig = new Mock<IConfiguration>();
            _mockEmail = new Mock<IEmailService>();
            _mockCache = new Mock<IMemoryCache>();

            _mockConfig.Setup(c => c["Jwt:Key"]).Returns("super-secret-key-for-testing-1234567890");
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
            _mockConfig.Setup(c => c["Jwt:ExpireMinutes"]).Returns("60");

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

        // UTCID01 - Abnormal: Email đã tồn tại trong hệ thống
        [Fact]
        public async Task SendOtpAsync_UTCID01_EmailAlreadyExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var existingUser = new User
            {
                Email = "email@gmail.com"
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync(existingUser);

            var request = new RegisterRequest
            {
                Email = "email@gmail.com"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.SendOtpAsync(request));

            Assert.Equal(ErrorMessages.EmailAlreadyRegistered, exception.Message);
        }

        // UTCID02 - Normal: Email đúng định dạng + Email Service OK
        [Fact]
        public async Task SendOtpAsync_UTCID02_ValidEmail_EmailServiceOk_ShouldSendOtpSuccessfully()
        {
            // Arrange - User chưa tồn tại
            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync((User?)null);

            // Email service gửi thành công (không throw)
            _mockEmail.Setup(e => e.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var request = new RegisterRequest
            {
                Email = "email@gmail.com"
            };

            // Act - Không throw là thành công
            await _authService.SendOtpAsync(request);

            // Assert - Verify email service đã được gọi đúng 1 lần
            _mockEmail.Verify(e => e.SendEmailAsync(
                "email@gmail.com",
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);

            // Verify cache đã được set
            _mockCache.Verify(c => c.CreateEntry(
                It.Is<object>(k => k.ToString().Contains("email@gmail.com"))),
                Times.Once);
        }

        // UTCID03 - Abnormal: Email đúng định dạng nhưng Email Service lỗi
        [Fact]
        public async Task SendOtpAsync_UTCID03_ValidEmail_EmailServiceFails_ShouldThrowException()
        {
            // Arrange - User chưa tồn tại
            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync((User?)null);

            // Email service throw exception (lỗi gửi mail)
            _mockEmail.Setup(e => e.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email sending failed"));

            var request = new RegisterRequest
            {
                Email = "email@gmail.com"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _authService.SendOtpAsync(request));

            Assert.Equal("Email sending failed", exception.Message);

            // Cache vẫn được tạo trước khi email lỗi
            _mockCache.Verify(c => c.CreateEntry(
                It.Is<object>(k => k.ToString().Contains("email@gmail.com"))),
                Times.Once);
        }

        // UTCID04 - Abnormal: Email tồn tại + Service OK nhưng trả về thất bại
        [Fact]
        public async Task SendOtpAsync_UTCID04_EmailExists_ServiceOk_ShouldThrowInvalidOperationException()
        {
            // Arrange - User đã tồn tại → bị chặn ngay từ đầu
            var existingUser = new User
            {
                Email = "email@gmail.com"
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync(existingUser);

            // Email service setup OK nhưng sẽ không bao giờ được gọi
            _mockEmail.Setup(e => e.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var request = new RegisterRequest
            {
                Email = "email@gmail.com"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.SendOtpAsync(request));

            Assert.Equal(ErrorMessages.EmailAlreadyRegistered, exception.Message);

            // Email service không được gọi vì bị chặn sớm
            _mockEmail.Verify(e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }
    }
}
