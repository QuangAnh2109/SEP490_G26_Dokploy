using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;

namespace Backend.UnitTest.CourseServiceTests
{
    public class RejectStudentAsyncTests : CourseTestBase
    {
        [Fact]
        public async Task Reject_WhenStudentNotPending_ShouldThrowException()
        {
            // Arrange
            _mockRepo.Setup(r => r.RejectStudentAsync(1, 2)).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _courseService.RejectStudentAsync(1, 2));
            Assert.Equal("Học sinh không tồn tại hoặc không ở trạng thái chờ duyệt.", ex.Message);
        }

        [Fact]
        public async Task Reject_WhenSuccessful_ShouldCallRepo()
        {
            // Arrange
            _mockRepo.Setup(r => r.RejectStudentAsync(1, 2)).ReturnsAsync(true);

            // Act
            await _courseService.RejectStudentAsync(1, 2);

            // Assert
            _mockRepo.Verify(r => r.RejectStudentAsync(1, 2), Times.Once);
        }
    }
}
