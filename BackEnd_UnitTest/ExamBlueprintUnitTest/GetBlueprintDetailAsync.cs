using Backend.DTOs.ExamBlueprint;
using Backend.Exceptions;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackEnd_UnitTest.ExamBlueprintUnitTest
{
    public class ExamBlueprintUnitTest
    {
        private readonly Mock<IExamBlueprintRepository> _mockRepo;
        private readonly ExamBlueprintService _service;

        public ExamBlueprintUnitTest()
        {
            _mockRepo = new Mock<IExamBlueprintRepository>();
            _service = new ExamBlueprintService(_mockRepo.Object);
        }

        // UTCID1 - Normal: Id hợp lệ + Có dữ liệu → Trả về BlueprintDetailDto thành công
        [Fact]
        public async Task GetBlueprintDetailAsync_UTCID01_ValidId_ExistingRecord_ShouldReturnBlueprintDetail()
        {
            // Arrange
            int id = 1;
            int currentUserId = 1;
            var expectedDetail = new BlueprintDetailDto { ExamBlueprintId = id };

            _mockRepo
                .Setup(r => r.GetBlueprintDetailAsync(id, currentUserId))
                .ReturnsAsync(expectedDetail);

            // Act
            var result = await _service.GetBlueprintDetailAsync(id, currentUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDetail.ExamBlueprintId, result.ExamBlueprintId);
            _mockRepo.Verify(r => r.GetBlueprintDetailAsync(id, currentUserId), Times.Once);
        }

        // UTCID2 - Abnormal: Id = 0 (không hợp lệ) → Ném ExamBlueprintValidationException
        [Fact]
        public async Task GetBlueprintDetailAsync_UTCID02_InvalidId_Zero_ShouldThrowValidationException()
        {
            // Arrange
            int id = 0;
            int currentUserId = 1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.GetBlueprintDetailAsync(id, currentUserId));

            // ExamBlueprintValidationException chứa danh sách errors, không phải trong .Message
            Assert.Contains("Id ma trận đề không hợp lệ.", exception.Errors);
            _mockRepo.Verify(r => r.GetBlueprintDetailAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        // UTCID3 - Normal: Id hợp lệ nhưng không tìm thấy bản ghi → Ném KeyNotFoundException
        [Fact]
        public async Task GetBlueprintDetailAsync_UTCID03_ValidId_RecordNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            int id = 1;
            int currentUserId = 1;

            _mockRepo
                .Setup(r => r.GetBlueprintDetailAsync(id, currentUserId))
                .ReturnsAsync((BlueprintDetailDto)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetBlueprintDetailAsync(id, currentUserId));

            Assert.Equal("Không tìm thấy ma trận đề.", exception.Message);
            _mockRepo.Verify(r => r.GetBlueprintDetailAsync(id, currentUserId), Times.Once);
        }

        // UTCID4 - Normal: Id hợp lệ + Có dữ liệu (Re-test) → Trả về BlueprintDetailDto thành công
        [Fact]
        public async Task GetBlueprintDetailAsync_UTCID04_ValidId_ExistingRecord_Retest_ShouldReturnBlueprintDetail()
        {
            // Arrange
            int id = 1;
            int currentUserId = 2;
            var expectedDetail = new BlueprintDetailDto { ExamBlueprintId = id };

            _mockRepo
                .Setup(r => r.GetBlueprintDetailAsync(id, currentUserId))
                .ReturnsAsync(expectedDetail);

            // Act
            var result = await _service.GetBlueprintDetailAsync(id, currentUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDetail.ExamBlueprintId, result.ExamBlueprintId);
            _mockRepo.Verify(r => r.GetBlueprintDetailAsync(id, currentUserId), Times.Once);
        }
    }
}
