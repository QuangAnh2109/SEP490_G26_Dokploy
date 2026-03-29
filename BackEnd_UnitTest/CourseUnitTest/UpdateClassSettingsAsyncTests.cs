using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.UnitTest.CourseServiceTests
{
    public class UpdateClassSettingsAsyncTests : CourseTestBase
    {
        [Fact]
        public async Task Update_WithEmptyName_ShouldThrowException()
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.UpdateClassSettingsAsync(1, "", 1));

            Assert.Equal("Tên lớp không được để trống.", ex.Message);
        }

        [Fact]
        public async Task Update_WhenCourseNotFound_ShouldThrowException()
        {
            // Arrange
            _mockRepo.Setup(r => r.UpdateClassSettingsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                     .ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.UpdateClassSettingsAsync(999, "New Name", 1));

            Assert.Equal("Không tìm thấy lớp học.", ex.Message);
        }

        [Fact]
        public async Task Update_ValidData_ShouldCallRepo()
        {
            // Arrange
            _mockRepo.Setup(r => r.UpdateClassSettingsAsync(1, "Valid Name", 1))
                     .ReturnsAsync(true);

            // Act
            await _courseService.UpdateClassSettingsAsync(1, "Valid Name", 1);

            // Assert
            _mockRepo.Verify(r => r.UpdateClassSettingsAsync(1, "Valid Name", 1), Times.Once);
        }
    }
}
