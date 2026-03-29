using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.UnitTest.CourseServiceTests
{
    public class AcceptInvitationAsyncTests : CourseTestBase
    {
        [Fact]
        public async Task Accept_InvalidTokenFormat_ShouldThrowException()
        {
            // Token không có dấu ":" sau khi giải mã
            string invalidToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("InvalidTokenNoColon"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.AcceptInvitationAsync(1, invalidToken));

            Assert.Equal("Token không hợp lệ.", ex.Message);
        }

        [Fact]
        public async Task Accept_ValidToken_ButRepoReturnsZeroRows_ShouldThrowException()
        {
            // Arrange: Tạo 1 token giả lập đúng cấu trúc {classId}:{stamp}
            var stamp = Convert.ToBase64String(new byte[] { 1, 2, 3 });
            var plainToken = $"1:{stamp}";
            var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainToken));

            _mockRepo.Setup(r => r.AcceptEmailInvitationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<byte[]>()))
                     .ReturnsAsync(0); // Không có dòng nào được update (hết hạn/sai stamp)

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.AcceptInvitationAsync(1, token));

            Assert.Equal("Link mời không hợp lệ hoặc đã hết hạn.", ex.Message);
        }

        [Fact]
        public async Task Accept_ValidToken_ShouldSucceed()
        {
            // Arrange
            var stampBytes = new byte[] { 1, 2, 3 };
            var stampBase64 = Convert.ToBase64String(stampBytes);
            var plainToken = $"10:{stampBase64}";
            var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainToken));

            _mockRepo.Setup(r => r.AcceptEmailInvitationAsync(10, 1, It.IsAny<byte[]>()))
                     .ReturnsAsync(1); // Thành công

            // Act
            await _courseService.AcceptInvitationAsync(1, token);

            // Assert
            _mockRepo.Verify(r => r.AcceptEmailInvitationAsync(10, 1, It.Is<byte[]>(b => b[0] == 1)), Times.Once);
        }
    }
}