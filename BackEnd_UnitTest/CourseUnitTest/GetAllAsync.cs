using Backend.DTOs.Course;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Backend_UnitTest
{
    public class CourseUnitTest
    {
        private readonly Mock<ICourseRepo> _mockRepo;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IConfiguration> _mockConfig;
        // Vì CourseService có dùng trực tiếp DbContext, ta dùng InMemoryDatabase cho nó
        private readonly MtcaSep490G26Context _context;
        private readonly CourseService _courseService;

        public CourseUnitTest()
        {
            _mockRepo = new Mock<ICourseRepo>();
            _mockEmail = new Mock<IEmailService>();
            _mockConfig = new Mock<IConfiguration>();

            // Khởi tạo DbContext ảo (InMemory) để không chạm vào DB thật
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<MtcaSep490G26Context>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MtcaSep490G26Context(options);

            _courseService = new CourseService(
                _mockRepo.Object,
                _context,
                _mockEmail.Object,
                _mockConfig.Object
            );
        }

        #region Test cho GetAllAsync
        [Fact]
        public async Task GetAllAsync_ShouldReturnList_WhenDataExists()
        {
            // Arrange (Chuẩn bị dữ liệu giả)
            var fakeCourses = new List<CourseDTO> {
                new CourseDTO { ClassId = 1, ClassName = "Lớp .NET" },
                new CourseDTO { ClassId = 2, ClassName = "Lớp React" }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(fakeCourses);

            // Act (Thực hiện hành động)
            var result = await _courseService.GetAllAsync();

            // Assert (Kiểm chứng)
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Lớp .NET", result[0].ClassName);
        }
        #endregion
        [Fact]
        public async Task GetAll_Scenario_Collection()
        {
            // --- CASE 1: Happy Path ---
            var data = new List<CourseDTO> { new CourseDTO { ClassId = 1 }, new CourseDTO { ClassId = 2 } };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(data);
            var result1 = await _courseService.GetAllAsync();
            Assert.Equal(2, result1.Count);

            // --- CASE 2: Empty Data ---
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CourseDTO>());
            var result2 = await _courseService.GetAllAsync();
            Assert.Empty(result2);

            // --- CASE 3: Repository Error ---
            _mockRepo.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB Error"));
            await Assert.ThrowsAsync<Exception>(() => _courseService.GetAllAsync());

            // --- CASE 4: Data Mapping Check ---
            var singleData = new List<CourseDTO> { new CourseDTO { ClassId = 10, ClassName = "Unit Test" } };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(singleData);
            var result4 = await _courseService.GetAllAsync();
            Assert.Equal("Unit Test", result4[0].ClassName);

            // --- CASE 5: GetByUserId (User has no courses) ---
            int userId = 999;
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(userId)).ReturnsAsync(new List<CourseDTO>());
            var result5 = await _courseService.GetCoursesForUserAsync(userId);
            Assert.NotNull(result5);
            Assert.Empty(result5);
        }
    }
}