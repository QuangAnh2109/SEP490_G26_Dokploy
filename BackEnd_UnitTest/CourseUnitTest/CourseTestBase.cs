using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Backend.Services.Implements;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Backend.Models;
using System;

namespace Backend.UnitTest
{
    // Dùng abstract để xUnit không cố gắng chạy class này như một bản test
    public abstract class CourseTestBase : IDisposable
    {
        // Các đối tượng Mock để các class con có thể Setup kịch bản
        protected readonly Mock<ICourseRepo> _mockRepo;
        protected readonly Mock<IEmailService> _mockEmail;
        protected readonly Mock<IConfiguration> _mockConfig;

        // DbContext thực tế chạy trên RAM (In-Memory)
        protected readonly MtcaSep490G26Context _context;

        // Đối tượng chính cần test
        protected readonly CourseService _courseService;

        protected CourseTestBase()
        {
            // 1. Khởi tạo các Mock
            _mockRepo = new Mock<ICourseRepo>();
            _mockEmail = new Mock<IEmailService>();
            _mockConfig = new Mock<IConfiguration>();

            // 2. Cấu hình InMemory Database với tên ngẫu nhiên cho mỗi lần test
            var options = new DbContextOptionsBuilder<MtcaSep490G26Context>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MtcaSep490G26Context(options);

            // 3. Khởi tạo Service với các đối tượng giả lập
            _courseService = new CourseService(
                _mockRepo.Object,
                _mockEmail.Object,
                _mockConfig.Object
            );
        }

        // Dọn dẹp Database sau khi test xong mỗi Class
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}