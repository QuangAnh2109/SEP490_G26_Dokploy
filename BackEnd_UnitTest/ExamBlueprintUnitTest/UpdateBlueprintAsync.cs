using Backend.Constants;
using Backend.DTOs.ExamBlueprint;
using Backend.Exceptions;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackEnd_UnitTest.ExamBlueprintUnitTest
{
    public class ExamBlueprintUpdateBlueprintUnitTest
    {
        private readonly Mock<IExamBlueprintRepository> _mockRepo;
        private readonly ExamBlueprintService _service;

        public ExamBlueprintUpdateBlueprintUnitTest()
        {
            _mockRepo = new Mock<IExamBlueprintRepository>();
            _service = new ExamBlueprintService(_mockRepo.Object);
        }
        private static CreateExamBlueprintRequest BuildValidRequest(int targetStatus = ExamBlueprintStatus.Approved, int targetTotal = 5)
        {
            return new CreateExamBlueprintRequest
            {
                Name = "Blueprint update",
                Description = "Description",
                SubjectId = 8,
                TargetStatus = targetStatus,
                TargetTotalQuestions = targetTotal,
                Rows = new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto
                    {
                        ChapterId = 1,
                        Difficulty = 1,
                        TotalQuestions = 5
                    }
                }
            };
        }
        private static List<ChapterOptionDto> BuildChapterOptions(int availableQuestions = 10)
        {
            return new List<ChapterOptionDto>
            {
                new ChapterOptionDto
                {
                    ChapterId = 1,
                    Name = "Chương 1",
                    AvailabilityByDifficulty = new List<ChapterAvailabilityDto>
                    {
                        new ChapterAvailabilityDto
                        {
                            Difficulty = 1,
                            AvailableQuestions = availableQuestions
                        }
                    }
                }
            };
        }

        // UTCID01 - Normal: Dữ liệu hợp lệ -> cập nhật thành công
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID01_ValidInput_ShouldReturnSuccessResponse()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest();

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId))
                     .ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                     .ReturnsAsync(BuildChapterOptions(10));
            _mockRepo.Setup(r => r.UpdateBlueprintAsync(
                    blueprintId,
                    currentUserId,
                    It.IsAny<ExamBlueprint>(),
                    It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = blueprintId,
                    Status = ExamBlueprintStatus.Approved,
                    UpdatedAtUtc = DateTime.UtcNow
                });

            // Act
            var result = await _service.UpdateBlueprintAsync(blueprintId, currentUserId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(blueprintId, result.ExamBlueprintId);
            Assert.Equal(ExamBlueprintStatus.Approved, result.Status);
            Assert.Equal("Cập nhật và xuất bản ma trận đề thành công.", result.Message);
            Assert.Empty(result.Warnings);
            _mockRepo.Verify(r => r.UpdateBlueprintAsync(
                blueprintId,
                currentUserId,
                It.IsAny<ExamBlueprint>(),
                It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Once);
        }

        // UTCID02 - Normal: currentUserId = 0 -> UnauthorizedAccessException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID02_CurrentUserIdIsZero_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 0;
            var request = BuildValidRequest();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            Assert.Equal("Invalid user or blueprint id.", exception.Message);
            _mockRepo.Verify(r => r.UpdateBlueprintAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<ExamBlueprint>(),
                It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID03 - Normal: Request không hợp lệ -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID03_InvalidRequest_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest();
            request.Name = "";

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId))
                     .ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                     .ReturnsAsync(BuildChapterOptions(10));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            Assert.Contains("Tên ma trận đề là bắt buộc.", exception.Errors);
            _mockRepo.Verify(r => r.UpdateBlueprintAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<ExamBlueprint>(),
                It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID04 - Normal: Blueprint không tồn tại -> KeyNotFoundException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID04_BlueprintNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            int blueprintId = 999;
            int currentUserId = 1;
            var request = BuildValidRequest();

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId))
                     .ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                     .ReturnsAsync(BuildChapterOptions(10));
            _mockRepo.Setup(r => r.UpdateBlueprintAsync(
                    blueprintId,
                    currentUserId,
                    It.IsAny<ExamBlueprint>(),
                    It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                .ReturnsAsync((ExamBlueprint?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            Assert.Equal("Không tìm thấy ma trận đề.", exception.Message);
        }

        // UTCID05 - Normal: Có warning nhưng vẫn cho phép lưu
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID05_ValidRequestWithWarning_ShouldSaveAndReturnWarnings()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(ExamBlueprintStatus.NotStarted, 10);

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId))
                     .ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                     .ReturnsAsync(BuildChapterOptions(10));
            _mockRepo.Setup(r => r.UpdateBlueprintAsync(
                    blueprintId,
                    currentUserId,
                    It.IsAny<ExamBlueprint>(),
                    It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = blueprintId,
                    Status = ExamBlueprintStatus.NotStarted,
                    UpdatedAtUtc = DateTime.UtcNow
                });

            // Act
            var result = await _service.UpdateBlueprintAsync(blueprintId, currentUserId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Cập nhật nháp ma trận đề thành công.", result.Message);
            Assert.Single(result.Warnings);
            Assert.Equal("TARGET_TOTAL_MISMATCH", result.Warnings[0].Code);
        }

        // UTCID06 - Normal: Re-test cập nhật thành công
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID06_Retest_ValidInput_ShouldReturnSuccessResponse()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 2;
            var request = BuildValidRequest();

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId))
                     .ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                     .ReturnsAsync(BuildChapterOptions(10));
            _mockRepo.Setup(r => r.UpdateBlueprintAsync(
                    blueprintId,
                    currentUserId,
                    It.IsAny<ExamBlueprint>(),
                    It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = blueprintId,
                    Status = ExamBlueprintStatus.Approved,
                    UpdatedAtUtc = DateTime.UtcNow
                });

            // Act
            var result = await _service.UpdateBlueprintAsync(blueprintId, currentUserId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(blueprintId, result.ExamBlueprintId);
            Assert.Equal(ExamBlueprintStatus.Approved, result.Status);
            Assert.Equal("Cập nhật và xuất bản ma trận đề thành công.", result.Message);
            Assert.Empty(result.Warnings);
        }
    }
}
