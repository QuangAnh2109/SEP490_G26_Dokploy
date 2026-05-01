using Backend.DTOs.Course;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Backend_UnitTest
{
    public class CourseUnitTest
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly CourseService _courseService;

        public CourseUnitTest()
        {
            _mockRepo = new Mock<ICourseRepository>();
            _mockEmail = new Mock<IEmailService>();
            _mockConfig = new Mock<IConfiguration>();
            var mockCurrentUser = new Mock<ICurrentUserService>();

            _courseService = new CourseService(
                _mockRepo.Object,
                _mockEmail.Object,
                _mockConfig.Object,
                mockCurrentUser.Object,
                TimeProvider.System
            );
        }

        [Fact]
        public async Task GetAllAsync_WhenDataExists_ShouldReturnFullList()
        {
            // Arrange
            var fakeData = new List<CourseDTO>
            {
                new CourseDTO { ClassId = 1, ClassName = "Course 1" },
                new CourseDTO { ClassId = 2, ClassName = "Course 2" }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(fakeData);

            // Act
            var result = await _courseService.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_WhenNoData_ShouldReturnEmptyList()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CourseDTO>());

            // Act
            var result = await _courseService.GetAllAsync();

            // Assert
            Assert.NotNull(result); // Không được null
            Assert.Empty(result);   // Phải rỗng
        }

        [Fact]
        public async Task GetAllAsync_ShouldMapCorrectDataFields()
        {
            // Arrange
            var fakeData = new List<CourseDTO>
            {
                new CourseDTO { ClassId = 99, ClassName = "Dotnet Testing" }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(fakeData);

            // Act
            var result = await _courseService.GetAllAsync();

            // Assert
            var firstCourse = result[0];
            Assert.Equal(99, firstCourse.ClassId);
            Assert.Equal("Dotnet Testing", firstCourse.ClassName);
        }

        [Fact]
        public async Task GetAllAsync_WhenRepoThrowsException_ShouldThrowSameException()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetAllAsync())
                     .ThrowsAsync(new System.Exception("Database connection failed"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<System.Exception>(() =>
                _courseService.GetAllAsync());

            Assert.Equal("Database connection failed", exception.Message);
        }

        [Fact]
        public async Task GetCoursesForUserAsync_WhenUserHasNoClass_ShouldReturnEmpty()
        {
            // Arrange
            int nonExistentUserId = 888;
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(nonExistentUserId))
                     .ReturnsAsync(new List<CourseDTO>());

            // Act
            var result = await _courseService.GetCoursesForUserAsync(nonExistentUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}