using Backend.DTOs.Course;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.UnitTest.CourseServiceTests
{
    public class GetCoursesForUserAsyncTests : CourseTestBase
    {
        [Fact]
        public async Task GetCoursesForUser_ShouldReturnListFromRepo()
        {
            // Arrange
            int userId = 1;
            var expected = new List<CourseDTO> { new CourseDTO { ClassId = 1, ClassName = "Lớp của tôi" } };
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(userId)).ReturnsAsync(expected);

            // Act
            var result = await _courseService.GetCoursesForUserAsync(userId);

            // Assert
            Assert.Single(result);
            Assert.Equal("Lớp của tôi", result[0].ClassName);
            _mockRepo.Verify(r => r.GetCoursesForUserAsync(userId), Times.Once);
        }
    }
}
