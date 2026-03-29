using Backend.DTOs.Course;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.UnitTest.CourseServiceTests
{
    public class GetPendingStudentsAsyncTests : CourseTestBase
    {
        [Fact]
        public async Task GetPendingStudents_ShouldReturnList()
        {
            // Arrange
            var pendingList = new List<StudentInClassDTO> { new StudentInClassDTO { StudentId = 1, FullName = "Student A" } };
            _mockRepo.Setup(r => r.GetPendingStudentsAsync(100)).ReturnsAsync(pendingList);

            // Act
            var result = await _courseService.GetPendingStudentsAsync(100);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal("Student A", result[0].FullName);
        }
    }
}
