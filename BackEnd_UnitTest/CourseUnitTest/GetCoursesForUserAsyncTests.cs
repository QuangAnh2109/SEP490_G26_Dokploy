using Backend.DTOs.Course;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Backend.UnitTest.CourseServiceTests
{
    public class GetCoursesForUserAsyncTests : CourseTestBase
    {
        /// <summary>
        /// Test Case 1: ID người dùng hợp lệ (id = 1) và đã tham gia khóa học
        /// Mong muốn: Return List có dữ liệu (T), Log: "success"
        /// </summary>
        [Fact]
        public async Task GetCoursesForUser_ValidId_UserJoined_ShouldReturnList()
        {
            // Arrange
            int userId = 1;
            var expectedData = new List<CourseDTO>
            {
                new CourseDTO { ClassId = 101, ClassName = "Mathematics 101" }
            };

            _mockRepo.Setup(r => r.GetCoursesForUserAsync(userId))
                     .ReturnsAsync(expectedData);

            // Act
            var result = await _courseService.GetCoursesForUserAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result); // Xác nhận có 1 khóa học như Arrange
            Assert.Equal("Mathematics 101", result[0].ClassName);
            _mockRepo.Verify(r => r.GetCoursesForUserAsync(userId), Times.Once);
        }

        /// <summary>
        /// Test Case 2: ID người dùng không hợp lệ (id = -1)
        /// Mong muốn: Return (T) - Thường trả về rỗng để tránh crash hoặc ném Exception tùy Logic Service
        /// </summary>
        [Fact]
        public async Task GetCoursesForUser_InvalidId_Negative_ShouldReturnEmptyOrException()
        {
            // Arrange
            int userId = -1;
            // Giả sử Repository trả về rỗng cho ID không hợp lệ
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(userId))
                     .ReturnsAsync(new List<CourseDTO>());

            // Act
            var result = await _courseService.GetCoursesForUserAsync(userId);

            // Assert
            Assert.Empty(result);
            _mockRepo.Verify(r => r.GetCoursesForUserAsync(userId), Times.Once);
        }

        /// <summary>
        /// Test Case 3: ID người dùng không tồn tại trong DB (id = 9999)
        /// Mong muốn: Return (F) - Trong logic C# thường là List rỗng
        /// </summary>
        [Fact]
        public async Task GetCoursesForUser_UserNotFound_ShouldReturnEmptyList()
        {
            // Arrange
            int userId = 9999;
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(userId))
                     .ReturnsAsync(new List<CourseDTO>());

            // Act
            var result = await _courseService.GetCoursesForUserAsync(userId);

            // Assert
            Assert.Empty(result);
            _mockRepo.Verify(r => r.GetCoursesForUserAsync(userId), Times.Once);
        }

        /// <summary>
        /// Test Case 4: ID người dùng hợp lệ (id = 2) nhưng chưa tham gia khóa học nào
        /// Mong muốn: Return (T) - Trả về danh sách rỗng
        /// </summary>
        [Fact]
        public async Task GetCoursesForUser_ValidId_NoCourses_ShouldReturnEmptyList()
        {
            // Arrange
            int userId = 2;
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(userId))
                     .ReturnsAsync(new List<CourseDTO>());

            // Act
            var result = await _courseService.GetCoursesForUserAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockRepo.Verify(r => r.GetCoursesForUserAsync(userId), Times.Once);
        }

        ///// <summary>
        ///// Kịch bản mở rộng: Lỗi kết nối Server (Pre-condition fail)
        ///// Mong muốn: Exception
        ///// </summary>
        //[Fact]
        //public async Task GetCoursesForUser_ServerError_ShouldThrowException()
        //{
        //    // Arrange
        //    _mockRepo.Setup(r => r.GetCoursesForUserAsync(It.IsAny<int>()))
        //             .ThrowsAsync(new Exception("Can connect with server failed"));

        //    // Act & Assert
        //    await Assert.ThrowsAsync<Exception>(() => _courseService.GetCoursesForUserAsync(1));
        //}
    }
}