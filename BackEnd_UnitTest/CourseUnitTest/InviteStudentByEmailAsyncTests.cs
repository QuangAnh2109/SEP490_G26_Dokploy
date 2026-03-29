using Backend.Constants;
using Backend.DTOs.Course;
using Backend.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.UnitTest.CourseServiceTests
{
    public class InviteStudentByEmailAsyncTests : CourseTestBase
    {
        [Fact]
        public async Task InviteByEmail_UserNotStudent_ShouldThrowException()
        {
            // Arrange
            var user = new User { Email = "teacher@test.com", Role = new Role { Name = "Teacher" } };
            _context.Users.Add(user); await _context.SaveChangesAsync();
            _mockConfig.Setup(c => c["FrontendSettings:BaseUrl"]).Returns("http://localhost");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _courseService.InviteStudentByEmailAsync(1, 1, "teacher@test.com"));
            Assert.Contains("vai trò là học sinh", ex.Message);
        }

        [Fact]
        public async Task InviteByEmail_ValidStudent_ShouldSendEmailAndReturnToken()
        {
            // Arrange
            var student = new User { UserId = 5, Email = "st@test.com", Role = new Role { Name = UserRoles.Student } };
            _context.Users.Add(student); await _context.SaveChangesAsync();

            _mockConfig.Setup(c => c["FrontendSettings:BaseUrl"]).Returns("http://localhost");
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new CourseDTO { ClassName = "Test Class" });
            _mockRepo.Setup(r => r.InviteStudentAsync(1, 5)).ReturnsAsync(new ClassMember { ConcurrencyStamp = new byte[] { 1, 2, 3 } });

            // Act
            var token = await _courseService.InviteStudentByEmailAsync(1, 1, "st@test.com");

            // Assert
            Assert.False(string.IsNullOrEmpty(token));
            _mockEmail.Verify(e => e.SendEmailAsync("st@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}