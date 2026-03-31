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
    public class AuthLogoutUnitTest
    {
        private readonly Mock<IAuthRepository> _mockAuthRepo;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly AuthService _authService;

        public AuthLogoutUnitTest()
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

        // UTCID01 - Normal: User tồn tại + Update SecurityStamp thành công
        //           → Đăng xuất thành công
        [Fact]
        public async Task LogoutAsync_UTCID01_UserExists_UpdateSecurityStamp_ShouldLogoutSuccessfully()
        {
            // Arrange
            var oldStamp = DateTime.UtcNow.AddDays(-1);
            var fakeUser = new User
            {
                UserId = 1,
                Email = "email@gmail.com",
                SecurityStamp = oldStamp
            };

            _mockAuthRepo.Setup(r => r.GetUserByIdAsync(1))
                         .ReturnsAsync(fakeUser);

            
            _mockAuthRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>()))
             .ReturnsAsync((User u) => u);

            // Act
            await _authService.LogoutAsync(1);

            // Assert - UpdateUserAsync được gọi đúng 1 lần
            _mockAuthRepo.Verify(r => r.UpdateUserAsync(
                It.Is<User>(u => u.UserId == 1)),
                Times.Once);

            // SecurityStamp phải được update (khác giá trị cũ)
            Assert.NotEqual(oldStamp, fakeUser.SecurityStamp);
        }

        // UTCID02 - Normal: User không tồn tại
        //           → Silent return, không throw, không update
        [Fact]
        public async Task LogoutAsync_UTCID02_UserNotFound_ShouldReturnSilently()
        {
            // Arrange
            _mockAuthRepo.Setup(r => r.GetUserByIdAsync(999))
                         .ReturnsAsync((User?)null);

            // Act - Không throw là đúng (source code check null trước)
            await _authService.LogoutAsync(999);

            // Assert - UpdateUserAsync không được gọi
            _mockAuthRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        }

        // UTCID03 - Normal: User tồn tại nhưng UpdateUserAsync throw Exception
        //           → Exception bubble up (source code không catch)
        [Fact]
        public async Task LogoutAsync_UTCID03_UserExists_UpdateFails_ShouldThrowException()
        {
            // Arrange
            var fakeUser = new User
            {
                UserId = 1,
                Email = "email@gmail.com",
                SecurityStamp = DateTime.UtcNow.AddDays(-1)
            };

            _mockAuthRepo.Setup(r => r.GetUserByIdAsync(1))
                         .ReturnsAsync(fakeUser);

            // UpdateUserAsync thất bại
            _mockAuthRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>()))
                         .ThrowsAsync(new Exception("Database update failed"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _authService.LogoutAsync(1));

            Assert.Equal("Database update failed", exception.Message);
        }

        // UTCID04 - Normal: User tồn tại + SecurityStamp được update
        //           → Verify SecurityStamp mới khác cũ (token cũ bị vô hiệu hóa)
        [Fact]
        public async Task LogoutAsync_UTCID04_UserExists_ShouldInvalidateOldTokens()
        {
            // Arrange
            var oldStamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var fakeUser = new User
            {
                UserId = 1,
                Email = "email@gmail.com",
                SecurityStamp = oldStamp
            };

            _mockAuthRepo.Setup(r => r.GetUserByIdAsync(1))
                         .ReturnsAsync(fakeUser);

            _mockAuthRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>()))
             .ReturnsAsync((User u) => u);

            // Act
            await _authService.LogoutAsync(1);

            // Assert - SecurityStamp mới phải sau thời điểm cũ
            Assert.True(fakeUser.SecurityStamp > oldStamp);

            // Verify UpdateUserAsync được gọi với SecurityStamp mới
            _mockAuthRepo.Verify(r => r.UpdateUserAsync(
                It.Is<User>(u =>
                    u.UserId == 1 &&
                    u.SecurityStamp > oldStamp)),
                Times.Once);
        }
    }
}
