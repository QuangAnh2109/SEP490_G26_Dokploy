using Backend.DTOs.Course;
using Backend.UnitTest;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackEnd_UnitTest.CourseUnitTest
{
    public class GetSubjectAsyncTest : CourseTestBase
    {
        [Fact]
        public async Task GetSubjects_WhenDataExists_ShouldReturnList()
        {
            // Arrange: Giả lập DB có 3 môn học
            var mockData = new List<CourseDTO> { new CourseDTO { ClassId = 1 }, new CourseDTO { ClassId = 2 } };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _courseService.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count); // Confirm Return T
        }

        [Fact]
        public async Task GetSubjects_WhenNoData_ShouldReturnEmptyList()
        {
            // Arrange: Giả lập DB trống
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CourseDTO>());

            // Act
            var result = await _courseService.GetAllAsync();

            // Assert
            Assert.Empty(result); // Confirm Return T (Empty List)
        }

        [Fact]
        public async Task GetSubjects_WhenDbError_ShouldThrowException()
        {
            // Arrange: Giả lập mất kết nối server
            _mockRepo.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("Database connection failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _courseService.GetAllAsync()); // Confirm Exception
        }
    }
}
