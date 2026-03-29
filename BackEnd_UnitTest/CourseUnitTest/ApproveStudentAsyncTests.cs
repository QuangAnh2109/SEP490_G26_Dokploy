using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.UnitTest.CourseServiceTests
{
    public class ApproveStudentAsyncTests : CourseTestBase
    {
        [Fact]
        public async Task Approve_StudentNotFoundOrNotPending_ShouldThrowException()
        {
            // Arrange
            _mockRepo.Setup(r => r.ApproveStudentAsync(1, 5)).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.ApproveStudentAsync(1, 5));

            Assert.Equal("Học sinh không tồn tại hoặc không ở trạng thái chờ duyệt.", ex.Message);
        }

        [Fact]
        public async Task Approve_ValidStudent_ShouldCallRepo()
        {
            // Arrange
            _mockRepo.Setup(r => r.ApproveStudentAsync(1, 5)).ReturnsAsync(true);

            // Act
            await _courseService.ApproveStudentAsync(1, 5);

            // Assert
            _mockRepo.Verify(r => r.ApproveStudentAsync(1, 5), Times.Once);
        }
    }
}
