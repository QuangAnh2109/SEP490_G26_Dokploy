using System;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs.Profile;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using Xunit;

namespace Backend_UnitTest
{
    public class UpdateProfileAsyncTests
    {
        private readonly Mock<IProfileRepository> _mockRepo;
        private readonly ProfileService _service;

        public UpdateProfileAsyncTests()
        {
            _mockRepo = new Mock<IProfileRepository>(MockBehavior.Strict);
            _service = new ProfileService(_mockRepo.Object, TimeProvider.System);
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenUserNotFound_ShouldReturnFalse_AndNotSave()
        {
            // Arrange
            int userId = 999;
            var dto = new UpdateProfileDTO
            {
                FullName = "Nguyen Van A",
                PhoneNumber = "0123456789",
                StudentId = null
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _service.UpdateProfileAsync(userId, dto);

            // Assert
            Assert.False(result);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
            _mockRepo.VerifyAll();
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenValidNonStudent_WithNullPhoneAndStudentId_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            int userId = 1;
            var user = new User
            {
                UserId = userId,
                Email = "teacher@example.com",
                RoleId = 1,
                Status = 1,
                FullName = "Old",
                PhoneNumber = "0999999999",
                StudentId = "SE000001"
            };

            var dto = new UpdateProfileDTO
            {
                FullName = "  Nguyen Van A  ",
                PhoneNumber = "   ",
                StudentId = null
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateProfileAsync(userId, dto);

            // Assert
            Assert.True(result);
            Assert.Equal("Nguyen Van A", user.FullName);
            Assert.Null(user.PhoneNumber);
            Assert.Null(user.StudentId);
            _mockRepo.Verify(r => r.UpdateUserAsync(user), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockRepo.VerifyAll();
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenFullNameNullOrWhitespace_ShouldThrowFullNameRequired()
        {
            // Arrange
            int userId = 2;
            var user = new User { UserId = userId, Email = "x@y.com", RoleId = 1, Status = 1 };
            var dto = new UpdateProfileDTO { FullName = "   ", PhoneNumber = "0123456789", StudentId = null };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateProfileAsync(userId, dto));

            // Assert
            Assert.Equal(ValidationMessages.FullNameRequired, ex.Message);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
            _mockRepo.VerifyAll();
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenFullNameInvalidFormat_ShouldThrowFullNameInvalid()
        {
            // Arrange
            int userId = 3;
            var user = new User { UserId = userId, Email = "x@y.com", RoleId = 1, Status = 1 };
            var dto = new UpdateProfileDTO { FullName = "Nguyen Van A1", PhoneNumber = "0123456789", StudentId = null };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateProfileAsync(userId, dto));

            // Assert
            Assert.Equal(ValidationMessages.FullNameInvalid, ex.Message);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
            _mockRepo.VerifyAll();
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenPhoneNumberInvalid_ShouldThrowPhoneNumberInvalid()
        {
            // Arrange
            int userId = 4;
            var user = new User { UserId = userId, Email = "x@y.com", RoleId = 1, Status = 1 };
            var dto = new UpdateProfileDTO { FullName = "Nguyen Van A", PhoneNumber = "1234567890", StudentId = null };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateProfileAsync(userId, dto));

            // Assert
            Assert.Equal(ValidationMessages.PhoneNumberInvalid, ex.Message);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
            _mockRepo.VerifyAll();
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenStudentRole_AndStudentIdMissing_ShouldThrowStudentIdRequiredForStudent()
        {
            // Arrange
            int userId = 5;
            var user = new User { UserId = userId, Email = "s@y.com", RoleId = 2, Status = 1 };
            var dto = new UpdateProfileDTO { FullName = "Nguyen Van A", PhoneNumber = "0123456789", StudentId = "   " };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateProfileAsync(userId, dto));

            // Assert
            Assert.Equal(ValidationMessages.StudentIdRequiredForStudent, ex.Message);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
            _mockRepo.VerifyAll();
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenStudentRole_AndStudentIdInvalid_ShouldThrowStudentIdInvalid()
        {
            // Arrange
            int userId = 6;
            var user = new User { UserId = userId, Email = "s@y.com", RoleId = 2, Status = 1 };
            var dto = new UpdateProfileDTO { FullName = "Nguyen Van A", PhoneNumber = "0123456789", StudentId = "H172047" };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateProfileAsync(userId, dto));

            // Assert
            Assert.Equal(ValidationMessages.StudentIdInvalid, ex.Message);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
            _mockRepo.VerifyAll();
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenStudentRole_WithNullPhoneAndNullStudentId_ShouldThrowStudentIdRequired()
        {
            // Arrange
            int userId = 61;
            var user = new User
            {
                UserId = userId,
                Email = "student@y.com",
                RoleId = 2,
                Status = 1,
                FullName = "Old Name",
                PhoneNumber = "0123456789",
                StudentId = "HE172047"
            };
            var dto = new UpdateProfileDTO
            {
                FullName = "Nguyen Van A",
                PhoneNumber = null,
                StudentId = null
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateProfileAsync(userId, dto));

            // Assert
            Assert.Equal(ValidationMessages.StudentIdRequiredForStudent, ex.Message);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
            _mockRepo.VerifyAll();
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenNonStudent_AndStudentIdProvidedInvalid_ShouldThrowStudentIdInvalid()
        {
            // Arrange
            int userId = 7;
            var user = new User { UserId = userId, Email = "t@y.com", RoleId = 1, Status = 1 };
            var dto = new UpdateProfileDTO { FullName = "Nguyen Van A", PhoneNumber = "0123456789", StudentId = "H172047" };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateProfileAsync(userId, dto));

            // Assert
            Assert.Equal(ValidationMessages.StudentIdInvalid, ex.Message);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
            _mockRepo.VerifyAll();
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenNonStudent_AndStudentIdProvidedValid_ShouldTrimAndUpdate()
        {
            // Arrange
            int userId = 8;
            var user = new User
            {
                UserId = userId,
                Email = "t@y.com",
                RoleId = 1,
                Status = 1,
                FullName = "Old",
                PhoneNumber = null,
                StudentId = null
            };

            var dto = new UpdateProfileDTO
            {
                FullName = "Nguyen Van A",
                PhoneNumber = " 0123456789 ",
                StudentId = "  HE172047 "
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateProfileAsync(userId, dto);

            // Assert
            Assert.True(result);
            Assert.Equal("Nguyen Van A", user.FullName);
            Assert.Equal("0123456789", user.PhoneNumber);
            Assert.Equal("HE172047", user.StudentId);
            _mockRepo.Verify(r => r.UpdateUserAsync(user), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockRepo.VerifyAll();
        }
    }
}

