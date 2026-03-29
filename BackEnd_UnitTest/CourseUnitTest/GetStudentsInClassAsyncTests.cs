using Backend.DTOs.Course;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.UnitTest.CourseServiceTests
{
    public class GetStudentsInClassAsyncTests : CourseTestBase
    {
        [Fact]
        public async Task GetStudentsInClass_ShouldReturnListFromRepo()
        {
            // Arrange
            int classId = 10;
            var expectedStudents = new List<StudentInClassDTO>
            {
                new StudentInClassDTO { StudentId = 1, FullName = "Nguyễn Văn A", Email = "a@gmail.com" },
                new StudentInClassDTO { StudentId = 2, FullName = "Trần Thị B", Email = "b@gmail.com" }
            };

            _mockRepo.Setup(r => r.GetStudentsInClassAsync(classId))
                     .ReturnsAsync(expectedStudents);

            // Act
            var result = await _courseService.GetStudentsInClassAsync(classId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Nguyễn Văn A", result[0].FullName);

            // Verify Repo được gọi đúng 1 lần với đúng ClassId
            _mockRepo.Verify(r => r.GetStudentsInClassAsync(classId), Times.Once);
        }

        [Fact]
        public async Task GetStudentsInClass_WhenNoStudents_ShouldReturnEmptyList()
        {
            // Arrange
            int classId = 99;
            _mockRepo.Setup(r => r.GetStudentsInClassAsync(classId))
                     .ReturnsAsync(new List<StudentInClassDTO>());

            // Act
            var result = await _courseService.GetStudentsInClassAsync(classId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
