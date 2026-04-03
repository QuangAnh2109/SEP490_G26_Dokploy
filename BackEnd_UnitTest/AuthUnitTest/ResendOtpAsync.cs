using Backend.Constants;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Backend.DTOs;
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
    public class AuthResendOtpUnitTest
    {
        private readonly Mock<IAuthRepository> _mockAuthRepo;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly AuthService _authService;


        public AuthResendOtpUnitTest()
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

            _authService = new AuthService(
                _mockAuthRepo.Object,
                _mockConfig.Object,
                _mockEmail.Object,
                _mockCache.Object
            );
        }

        // UTCID01 - Abnormal: Không có Cache
        [Fact]
        public async Task ResendOtpAsync_UTCID01_NoCacheExists_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange - Cache không tồn tại → TryGetValue trả về false
            object cacheValue = null;
            _mockCache.Setup(c => c.TryGetValue(
                    It.Is<object>(k => k.ToString() == "OTP_email@gmail.com"),
                    out cacheValue))
                .Returns(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.ResendOtpAsync("email@gmail.com"));

            Assert.Equal(ErrorMessages.OtpExpiredOrNotExists, exception.Message);
        }

        // UTCID02 - Abnormal: Cache tồn tại nhưng dữ liệu null
        [Fact]
        public async Task ResendOtpAsync_UTCID02_CacheExistsButDataNull_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange - TryGetValue trả về true nhưng out value là null
            object cacheValue = null;
            _mockCache.Setup(c => c.TryGetValue(
                    It.Is<object>(k => k.ToString() == "OTP_email@gmail.com"),
                    out cacheValue))
                .Returns(true); // Tìm thấy key nhưng value = null

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.ResendOtpAsync("email@gmail.com"));
        }

        // UTCID03 - Normal: Mọi điều kiện OK nhưng Email Service lỗi
        [Fact]
        public async Task ResendOtpAsync_UTCID03_AllConditionsOk_EmailServiceFails_ShouldThrowException()
        {
            // Arrange - Cache có dữ liệu hợp lệ
            var fakeRegRequest = new RegisterRequest { Email = "email@gmail.com" }; 
            var fakeCacheData = new OtpCacheData { Request = fakeRegRequest, Otp = "123456" };


            object cacheValue = fakeCacheData;
            _mockCache.Setup(c => c.TryGetValue(
                    It.Is<object>(k => k.ToString() == "OTP_email@gmail.com"),
                    out cacheValue))
                .Returns(true);

            // Email service throw exception
            _mockEmail.Setup(e => e.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email sending failed"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _authService.ResendOtpAsync("email@gmail.com"));

            Assert.Equal("Email sending failed", exception.Message);

            // Cache đã được update trước khi email lỗi
            _mockCache.Verify(c => c.CreateEntry(
                It.Is<object>(k => k.ToString() == "OTP_email@gmail.com")),
                Times.Once);
        }

        // UTCID04 - Abnormal: Cache OK nhưng Email Service lỗi
        [Fact]
        public async Task ResendOtpAsync_UTCID04_CacheOk_EmailServiceFails_ShouldThrowException()
        {
            // Arrange
            var fakeRegRequest = new RegisterRequest { Email = "email@gmail.com" };
            var fakeCacheData = new OtpCacheData { Request = fakeRegRequest, Otp = "654321" };


            object cacheValue = fakeCacheData;
            _mockCache.Setup(c => c.TryGetValue(
                    It.Is<object>(k => k.ToString() == "OTP_email@gmail.com"),
                    out cacheValue))
                .Returns(true);

            _mockEmail.Setup(e => e.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP connection error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _authService.ResendOtpAsync("email@gmail.com"));

            Assert.NotNull(exception.Message);

            // Verify email service đã được gọi (nhưng lỗi)
            _mockEmail.Verify(e => e.SendEmailAsync(
                "email@gmail.com",
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        // UTCID05 - Abnormal: Email đầu vào là null
        [Fact]
        public async Task ResendOtpAsync_UTCID05_NullEmail_ShouldThrowException()
        {
            // Arrange - Email null → cache key = "OTP_" → TryGetValue trả về false
            object cacheValue = null;
            _mockCache.Setup(c => c.TryGetValue(
                    It.IsAny<object>(),
                    out cacheValue))
                .Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.ResendOtpAsync(null));
        }

        // UTCID06 - Normal: Cache hợp lệ + gửi email thành công → method hoàn tất (đóng async state machine)
        [Fact]
        public async Task ResendOtpAsync_UTCID06_ValidCache_EmailOk_ShouldComplete()
        {
            var fakeRegRequest = new RegisterRequest { Email = "ok@gmail.com" };
            var fakeCacheData = new OtpCacheData { Request = fakeRegRequest, Otp = "111222" };

            object cacheValue = fakeCacheData;
            _mockCache.Setup(c => c.TryGetValue(
                    It.Is<object>(k => k.ToString() == "OTP_ok@gmail.com"),
                    out cacheValue))
                .Returns(true);

            _mockEmail.Setup(e => e.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _authService.ResendOtpAsync("ok@gmail.com");

            _mockEmail.Verify(e => e.SendEmailAsync(
                "ok@gmail.com",
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        public class OtpCacheData
        {
            public RegisterRequest Request { get; set; }
            public string Otp { get; set; }
        }
    }
}
