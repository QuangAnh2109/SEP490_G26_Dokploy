using Backend.DTOs.Profile;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Constants;
using Moq;
using Xunit;

namespace Backend_UnitTest
{
    public class ProfileServiceTests
    {
        private readonly Mock<IProfileRepository> _mockRepo;
        private readonly ProfileService _service;

        public ProfileServiceTests()
        {
            _mockRepo = new Mock<IProfileRepository>();
            _service = new ProfileService(_mockRepo.Object, TimeProvider.System);
        }

        // Sheet mapping (ảnh):
        // - GetProfileAsync: UTCID01 (user tồn tại), UTCID02 (user không tồn tại)
        // - UpdateProfileAsync: UTCID01 (DTO hợp lệ), UTCID02 (user không tồn tại), UTCID03 (DTO null ở field update)

        [Fact(DisplayName = "GetProfileAsync - UTCID01 - User tồn tại trong DB => trả UserProfileDTO")]
        public async Task GetProfileAsync_UTCID01_UserExists_ShouldReturnMappedDto()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Nguyen Van A",
                PhoneNumber = "0123456789",
                StudentId = "HE172047",
                RoleId = 2,
                Status = 1
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            // Act
            var result = await _service.GetProfileAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.UserId, result!.UserId);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.FullName, result.FullName);
            Assert.Equal(user.PhoneNumber, result.PhoneNumber);
            Assert.Equal(user.StudentId, result.StudentId);
            Assert.Equal(user.RoleId, result.RoleId);
            Assert.Equal(user.Status, result.Status);

            _mockRepo.Verify(r => r.GetUserByIdAsync(1), Times.Once);
        }

        [Fact(DisplayName = "GetProfileAsync - UTCID02 - User không tồn tại trong DB => trả null")]
        public async Task GetProfileAsync_UTCID02_UserNotFound_ShouldReturnNull()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetUserByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

            // Act
            var result = await _service.GetProfileAsync(999);

            // Assert
            Assert.Null(result);
            _mockRepo.Verify(r => r.GetUserByIdAsync(999), Times.Once);
        }

        [Fact(DisplayName = "UpdateProfileAsync - UTCID01 - DTO hợp lệ + user tồn tại => return true, DB updated")]
        public async Task UpdateProfileAsync_UTCID01_UserExists_ValidDto_ShouldUpdateFieldsAndSave()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Old Name",
                PhoneNumber = "000",
                StudentId = "OLD",
                RoleId = 2,
                Status = 1
            };

            var dto = new UpdateProfileDTO
            {
                FullName = "New Name",
                PhoneNumber = "0123456789",
                StudentId = "HE172047"
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var ok = await _service.UpdateProfileAsync(1, dto);

            // Assert
            Assert.True(ok);
            Assert.Equal(dto.FullName, user.FullName);
            Assert.Equal(dto.PhoneNumber, user.PhoneNumber);
            Assert.Equal(dto.StudentId, user.StudentId);

            _mockRepo.Verify(r => r.GetUserByIdAsync(1), Times.Once);
            _mockRepo.Verify(r => r.UpdateUserAsync(user), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact(DisplayName = "UpdateProfileAsync - UTCID02 - User không tồn tại => return false, không save")]
        public async Task UpdateProfileAsync_UTCID02_UserNotFound_ShouldReturnFalse_AndNotSave()
        {
            // Arrange
            var dto = new UpdateProfileDTO
            {
                FullName = "New Name",
                PhoneNumber = "0123456789",
                StudentId = "HE172047"
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(999)).ReturnsAsync((User?)null);

            // Act
            var ok = await _service.UpdateProfileAsync(999, dto);

            // Assert
            Assert.False(ok);
            _mockRepo.Verify(r => r.GetUserByIdAsync(999), Times.Once);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact(DisplayName = "UpdateProfileAsync - UTCID03 - DTO FullName null/blank => throw FullNameRequired, không save")]
        public async Task UpdateProfileAsync_UTCID03_UserExists_DtoWithNullFullName_ShouldThrowAndNotSave()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Old Name",
                PhoneNumber = "000",
                StudentId = "OLD",
                RoleId = 2,
                Status = 1
            };

            var dto = new UpdateProfileDTO
            {
                FullName = null,
                PhoneNumber = null,
                StudentId = null
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateProfileAsync(1, dto));

            // Assert
            Assert.Equal(ValidationMessages.FullNameRequired, ex.Message);

            _mockRepo.Verify(r => r.GetUserByIdAsync(1), Times.Once);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }
    }
}

