using Backend.DTOs.Course;
using Backend.Models;
using Backend.UnitTest;
using Moq;

namespace BackendUnitTest_CourseServiceTests
{
    public class CreateCourseAsyncTests : CourseTestBase
    {
        [Fact]
        public async Task CreateCourseAsync_ValidData_ShouldReturnCreatedCourse()
        {
            // Arrange
            var dto = new CreateCourseRequestDTO { ClassName = "Math", Semester = "fall 2024", SubjectId = 1 };
            _mockRepo.Setup(r => r.GetDuplicateClassErrorAsync(1, "Math", "FALL 2024", 1)).ReturnsAsync((string?)null);
            _mockRepo.Setup(r => r.CreateCourseAsync(It.IsAny<Class>())).ReturnsAsync(new CourseDTO { ClassName = "Math" });

            // Act
            var result = await _courseService.CreateCourseAsync(1, dto);

            // Assert
            Assert.Equal("Math", result.ClassName);
            _mockRepo.Verify(r => r.CreateCourseAsync(It.Is<Class>(c => c.Semester == "FALL 2024")), Times.Once);
        }

        [Fact]
        public async Task CreateCourseAsync_DuplicateData_ShouldThrowException()
        {
            // Arrange
            var dto = new CreateCourseRequestDTO { ClassName = "Duplicate" };
            _mockRepo.Setup(r => r.GetDuplicateClassErrorAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                     .ReturnsAsync("Lớp đã tồn tại");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _courseService.CreateCourseAsync(1, dto));
            Assert.Equal("Lớp đã tồn tại", ex.Message);
        }
    }
}