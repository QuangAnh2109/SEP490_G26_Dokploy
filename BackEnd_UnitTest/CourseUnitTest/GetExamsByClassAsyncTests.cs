using Backend.DTOs.Course;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.UnitTest.CourseServiceTests
{
    public class GetExamsByClassAsyncTests : CourseTestBase
    {
        [Fact]
        public async Task GetExamsByClass_ShouldReturnExams()
        {
            // Arrange
            var exams = new List<ExamInCourseDTO> { new ExamInCourseDTO { ExamId = 1, Title = "Final Exam" } };
            _mockRepo.Setup(r => r.GetExamsByClassAsync(1)).ReturnsAsync(exams);

            // Act
            var result = await _courseService.GetExamsByClassAsync(1);

            // Assert
            Assert.Single(result);
            Assert.Equal("Final Exam", result[0].Title);
        }
    }
}
