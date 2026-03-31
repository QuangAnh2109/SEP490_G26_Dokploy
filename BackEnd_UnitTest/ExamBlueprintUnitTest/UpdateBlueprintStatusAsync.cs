using Backend.Constants;
using Backend.Exceptions;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackEnd_UnitTest.ExamBlueprintUnitTest
{
    public class ExamBlueprintUpdateStatusUnitTest
    {
        private readonly Mock<IExamBlueprintRepository> _mockRepo;
        private readonly ExamBlueprintService _service;

        public ExamBlueprintUpdateStatusUnitTest()
        {
            _mockRepo = new Mock<IExamBlueprintRepository>();
            _service = new ExamBlueprintService(_mockRepo.Object);
        }

        // UTCID01 - Normal: Id = 1 và Status = Archived -> cập nhật thành công
        [Fact]
        public async Task UpdateBlueprintStatusAsync_UTCID01_ValidIdAndArchivedStatus_ShouldCallRepositoryAndReturnCount()
        {
            // Arrange
            var ids = new List<int> { 1 };
            int currentUserId = 1;
            int status = ExamBlueprintStatus.Archived;

            _mockRepo.Setup(r => r.UpdateBlueprintStatusAsync(
                    It.IsAny<IEnumerable<int>>(),
                    currentUserId,
                    status))
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateBlueprintStatusAsync(ids, currentUserId, status);

            // Assert
            Assert.Equal(1, result);
            _mockRepo.Verify(r => r.UpdateBlueprintStatusAsync(
                It.Is<IEnumerable<int>>(x => x.Contains(1) && x.Count() == 1),
                currentUserId,
                status), Times.Once);
        }

        // UTCID02 - Normal: Id = 1 nhưng Status không phải Archived -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintStatusAsync_UTCID02_StatusNotArchived_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            var ids = new List<int> { 1 };
            int currentUserId = 1;
            int status = ExamBlueprintStatus.Approved; // khác Archived

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintStatusAsync(ids, currentUserId, status));

            Assert.Contains("Chỉ hỗ trợ chuyển trạng thái sang Lưu trữ.", exception.Errors);
            _mockRepo.Verify(r => r.UpdateBlueprintStatusAsync(
                It.IsAny<IEnumerable<int>>(),
                It.IsAny<int>(),
                It.IsAny<int>()), Times.Never);
        }

        // UTCID03 - Normal: Id = 0 và Status = Archived -> không ném exception, trả về 0, không gọi repository
        [Fact]
        public async Task UpdateBlueprintStatusAsync_UTCID03_IdIsZero_ShouldReturnZeroAndNotCallRepository()
        {
            // Arrange
            var ids = new List<int> { 0 };
            int currentUserId = 1;
            int status = ExamBlueprintStatus.Archived;

            // Act
            var result = await _service.UpdateBlueprintStatusAsync(ids, currentUserId, status);

            // Assert
            Assert.Equal(0, result);
            _mockRepo.Verify(r => r.UpdateBlueprintStatusAsync(
                It.IsAny<IEnumerable<int>>(),
                It.IsAny<int>(),
                It.IsAny<int>()), Times.Never);
        }

        // UTCID04 - Normal: Id = null (hoặc danh sách rỗng) và Status = Archived -> không ném exception, trả về 0
        [Fact]
        public async Task UpdateBlueprintStatusAsync_UTCID04_IdsIsNullOrEmpty_ShouldReturnZeroAndNotCallRepository()
        {
            // Arrange
            List<int>? ids = null;
            int currentUserId = 1;
            int status = ExamBlueprintStatus.Archived;

            // Act
            var resultWithNull = await _service.UpdateBlueprintStatusAsync(ids ?? new List<int>(), currentUserId, status);

            // Assert
            Assert.Equal(0, resultWithNull);

            // Re-check với danh sách rỗng
            var resultWithEmpty = await _service.UpdateBlueprintStatusAsync(new List<int>(), currentUserId, status);
            Assert.Equal(0, resultWithEmpty);

            _mockRepo.Verify(r => r.UpdateBlueprintStatusAsync(
                It.IsAny<IEnumerable<int>>(),
                It.IsAny<int>(),
                It.IsAny<int>()), Times.Never);
        }
    }
}
