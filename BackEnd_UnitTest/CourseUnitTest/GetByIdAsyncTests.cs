using Backend.DTOs.Course;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.UnitTest.CourseServiceTests
{
    public class GetByIdAsyncTests : CourseTestBase
    {
        [Fact]
        public async Task GetById_WhenFound_ShouldReturnCourse()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new CourseDTO { ClassId = 1 });

            // Act
            var result = await _courseService.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ClassId);
        }

        [Fact]
        public async Task GetById_WhenNotFound_ShouldReturnNull()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((CourseDTO?)null);

            // Act
            var result = await _courseService.GetByIdAsync(99);

            // Assert
            Assert.Null(result);
        }
    }
}
