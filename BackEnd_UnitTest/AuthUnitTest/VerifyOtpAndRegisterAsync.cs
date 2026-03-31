using Backend.Constants;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using static Backend_UnitTest.AuthUnitTest.AuthResendOtpUnitTest;

namespace Backend_UnitTest.AuthUnitTest
{
    public class AuthVerifyOtpUnitTest
    {
        private readonly Mock<IAuthRepository> _mockAuthRepo;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly AuthService _authService;

        public AuthVerifyOtpUnitTest()
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

            // Mock Remove (không làm gì)
            _mockCache.Setup(c => c.Remove(It.IsAny<object>()));

            _authService = new AuthService(
                _mockAuthRepo.Object,
                _mockConfig.Object,
                _mockEmail.Object,
                _mockCache.Object
            );
        }

        // Helper setup cache hợp lệ
        private void SetupValidCache(string email, string otp, string password = "password123", int roleId = 1)
        {
            var fakeRegRequest = new RegisterRequest
            {
                Email = email,
                Password = password,
                RoleId = roleId
            };
            var fakeCacheData = new OtpCacheData { Request = fakeRegRequest, Otp = otp };

            object cacheValue = fakeCacheData;
            _mockCache.Setup(c => c.TryGetValue(
                    It.Is<object>(k => k.ToString() == $"OTP_{email}"),
                    out cacheValue))
                .Returns(true);
        }

        // Helper setup cache không tồn tại
        private void SetupEmptyCache(string email)
        {
            object cacheValue = null;
            _mockCache.Setup(c => c.TryGetValue(
                    It.Is<object>(k => k.ToString() == $"OTP_{email}"),
                    out cacheValue))
                .Returns(false);
        }

        // UTCID01 - Normal: OTP đúng + User mới → Đăng ký thành công
        [Fact]
        public async Task VerifyOtpAndRegisterAsync_UTCID01_ValidOtp_NewUser_ShouldReturnLoginResponse()
        {
            // Arrange
            SetupValidCache("email@gmail.com", "123456");

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync((User?)null);

            _mockAuthRepo.Setup(r => r.AddUserAsync(It.IsAny<User>()))
                         .ReturnsAsync((User u) => u);

            var request = new VerifyOtpRequest
            {
                Email = "email@gmail.com",
                OtpCode = "123456"
            };

            // Act
            var result = await _authService.VerifyOtpAndRegisterAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.NotNull(result.RefreshToken);
            Assert.Equal("email@gmail.com", result.Email);
            Assert.Equal("User", result.RoleName);

            // OTP phải bị xóa khỏi cache sau khi verify
            _mockCache.Verify(c => c.Remove(
                It.Is<object>(k => k.ToString() == "OTP_email@gmail.com")),
                Times.AtLeastOnce);
        }

        // UTCID02 - Abnormal: OTP sai + Role null
        [Fact]
        public async Task VerifyOtpAndRegisterAsync_UTCID02_WrongOtp_NullRole_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange - Cache có OTP "123456" nhưng request gửi OTP sai
            SetupValidCache("email@gmail.com", "123456");

            var request = new VerifyOtpRequest
            {
                Email = "email@gmail.com",
                OtpCode = "999999" // OTP sai
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.VerifyOtpAndRegisterAsync(request));

            Assert.Equal(ErrorMessages.InvalidOtp, exception.Message);
        }

        // UTCID03 - Abnormal: OTP hết hạn hoặc không tồn tại trong Cache
        [Fact]
        public async Task VerifyOtpAndRegisterAsync_UTCID03_OtpExpiredOrNotExists_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange - Cache không có dữ liệu
            SetupEmptyCache("email@gmail.com");

            var request = new VerifyOtpRequest
            {
                Email = "email@gmail.com",
                OtpCode = "123456"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.VerifyOtpAndRegisterAsync(request));

            Assert.Equal(ErrorMessages.OtpExpiredOrNotExists, exception.Message);
        }

        // UTCID04 - Normal: OTP đúng nhưng User đã tồn tại
        [Fact]
        public async Task VerifyOtpAndRegisterAsync_UTCID04_ValidOtp_UserAlreadyExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            SetupValidCache("email@gmail.com", "123456");

            var existingUser = new User
            {
                Email = "email@gmail.com",
                Role = new Role { Name = "User" }
            };

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync(existingUser);

            var request = new VerifyOtpRequest
            {
                Email = "email@gmail.com",
                OtpCode = "123456"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.VerifyOtpAndRegisterAsync(request));

            Assert.Equal(ErrorMessages.UserAlreadyExists, exception.Message);
        }

        // UTCID05 - Normal: OTP đúng + Role null
        [Fact]
        public async Task VerifyOtpAndRegisterAsync_UTCID05_ValidOtp_NullRole_ShouldReturnDefaultRoleName()
        {
            // Arrange
            SetupValidCache("email@gmail.com", "123456");

            _mockAuthRepo.Setup(r => r.GetUserByEmailAsync("email@gmail.com"))
                         .ReturnsAsync((User?)null);

            // AddUserAsync trả về user với Role = null
            _mockAuthRepo.Setup(r => r.AddUserAsync(It.IsAny<User>()))
                         .ReturnsAsync((User u) =>
                         {
                             u.Role = null; // Role null
                             return u;
                         });

            var request = new VerifyOtpRequest
            {
                Email = "email@gmail.com",
                OtpCode = "123456"
            };

            // Act
            var result = await _authService.VerifyOtpAndRegisterAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("User", result.RoleName); // fallback khi Role null
            Assert.Equal("email@gmail.com", result.Email);
            Assert.NotNull(result.Token);
        }

        // UTCID06 - Normal: Email sai định dạng
        [Fact]
        public async Task VerifyOtpAndRegisterAsync_UTCID06_InvalidEmailFormat_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange - Email sai định dạng → cache key không khớp → TryGetValue false
            object cacheValue = null;
            _mockCache.Setup(c => c.TryGetValue(
                    It.IsAny<object>(),
                    out cacheValue))
                .Returns(false);

            var request = new VerifyOtpRequest
            {
                Email = "sai-dinh-dang",
                OtpCode = "123456"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.VerifyOtpAndRegisterAsync(request));

            Assert.Equal(ErrorMessages.OtpExpiredOrNotExists, exception.Message);
        }
    }
}
