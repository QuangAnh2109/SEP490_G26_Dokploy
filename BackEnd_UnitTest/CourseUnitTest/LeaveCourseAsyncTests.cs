using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.UnitTest.CourseServiceTests
{
    public class LeaveCourseAsyncTests : CourseTestBase
    {
        [Fact]
        public async Task LeaveCourse_ShouldInvokeRepoOnce()
        {
            // Act
            await _courseService.LeaveCourseAsync(1, 10);

            // Assert
            _mockRepo.Verify(r => r.LeaveClassAsync(1, 10), Times.Once);
        }
    }
}
