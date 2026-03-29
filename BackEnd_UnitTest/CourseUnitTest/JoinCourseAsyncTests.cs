using Backend.Models;
using Backend.UnitTest;
using Moq;

namespace Backend_UnitTest.CourseServiceTests
{
    public class JoinCourseAsyncTests : CourseTestBase
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task JoinCourseAsync_EmptyInviteCode_ShouldThrowException(string code)
        {
            var ex = await Assert.ThrowsAsync<Exception>(() => _courseService.JoinCourseAsync(1, code));
            Assert.Equal("Mã mời không thể trống.", ex.Message);
        }

        [Fact]
        public async Task JoinCourseAsync_InvalidCode_ShouldThrowException()
        {
            _mockRepo.Setup(r => r.GetClassByInviteCodeAsync("WRONG")).ReturnsAsync((Class?)null);
            var ex = await Assert.ThrowsAsync<Exception>(() => _courseService.JoinCourseAsync(1, "WRONG"));
            Assert.Equal("Mã mời không chính xác hoặc lớp học đã bị đóng.", ex.Message);
        }

        [Fact]
        public async Task JoinCourseAsync_AlreadyInClass_ShouldThrowException()
        {
            var course = new Class { ClassId = 10 };
            _mockRepo.Setup(r => r.GetClassByInviteCodeAsync("VALID")).ReturnsAsync(course);
            _mockRepo.Setup(r => r.IsUserInClassAsync(10, 1)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<Exception>(() => _courseService.JoinCourseAsync(1, "VALID"));
            Assert.Equal("Bạn đã ở trong lớp học này rồi.", ex.Message);
        }
    }
}