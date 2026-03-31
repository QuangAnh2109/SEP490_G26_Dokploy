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
    public class ExamBlueprintCreateBlueprintUnitTest
    {
        private readonly Mock<IExamBlueprintRepository> _mockRepo;
        private readonly ExamBlueprintService _service;

        public ExamBlueprintCreateBlueprintUnitTest()
        {
            _mockRepo = new Mock<IExamBlueprintRepository>();
            _service = new ExamBlueprintService(_mockRepo.Object);
        }

        private static CreateExamBlueprintRequest BuildValidRequest(
            int targetStatus = ExamBlueprintStatus.Approved,
            int targetTotalQuestions = 5,
            int totalQuestionsInRow = 5)
        {
            return new CreateExamBlueprintRequest
            {
                Name = "Blueprint 1",
                Description = "Description",
                SubjectId = 8,
                TargetStatus = targetStatus,
                TargetTotalQuestions = targetTotalQuestions,
                Rows = new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto
                    {
                        ChapterId = 1,
                        Difficulty = 1,
                        TotalQuestions = totalQuestionsInRow
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

        // UTCID01 - Normal: Mọi dữ liệu và quyền hạn đều hợp lệ -> tạo thành công
        [Fact]
        public async Task CreateBlueprintAsync_UTCID01_ValidInput_ShouldCreateSuccessfully()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest();

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId))
                     .ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                     .ReturnsAsync(BuildChapterOptions(10));
            _mockRepo.Setup(r => r.CreateBlueprintAsync(
                    It.IsAny<ExamBlueprint>(),
                    It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = 1,
                    Status = ExamBlueprintStatus.Approved,
                    UpdatedAtUtc = DateTime.UtcNow
                });

            // Act
            var result = await _service.CreateBlueprintAsync(currentUserId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ExamBlueprintId);
            Assert.Equal(ExamBlueprintStatus.Approved, result.Status);
            Assert.Equal("Tạo và xuất bản ma trận đề thành công.", result.Message);
            Assert.Empty(result.Warnings);
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(),
                It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Once);
        }

        // UTCID02 - Normal: currentUserId = 0 (Sai người dùng) -> UnauthorizedAccessException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID02_InvalidUser_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            int currentUserId = 0;
            var request = BuildValidRequest();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            Assert.Equal("Invalid user.", exception.Message);
            _mockRepo.Verify(r => r.SubjectExistsAsync(It.IsAny<int>()), Times.Never);
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(),
                It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID03 - Normal: Dữ liệu Input không hợp lệ -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID03_InvalidInput_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest();
            request.Name = ""; // Name không hợp lệ

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            Assert.Contains("Tên ma trận đề là bắt buộc.", exception.Errors);
            _mockRepo.Verify(r => r.SubjectExistsAsync(It.IsAny<int>()), Times.Never);
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(),
                It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID04 - Normal: Môn học (Subject) không tồn tại -> KeyNotFoundException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID04_SubjectNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest();

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId))
                     .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            Assert.Equal("Không tìm thấy môn học.", exception.Message);
            _mockRepo.Verify(r => r.GetChaptersBySubjectAsync(It.IsAny<int>()), Times.Never);
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(),
                It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID05 - Normal: Dữ liệu Rows không hợp lệ -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID05_InvalidRows_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest();
            request.Rows = new List<CreateExamBlueprintRowDto>
            {
                new CreateExamBlueprintRowDto
                {
                    ChapterId = 0, // không hợp lệ
                    Difficulty = 1,
                    TotalQuestions = 5
                }
            };

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId))
                     .ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                     .ReturnsAsync(BuildChapterOptions(10));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            Assert.Contains("Mỗi dòng ma trận phải có chương hợp lệ.", exception.Errors);
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(),
                It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID06 - Normal: Cấu trúc dữ liệu đúng (Re-test thành công) -> tạo thành công
        [Fact]
        public async Task CreateBlueprintAsync_UTCID06_ValidInput_Retest_ShouldCreateSuccessfully()
        {
            // Arrange
            int currentUserId = 2;
            var request = BuildValidRequest();

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId))
                     .ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                     .ReturnsAsync(BuildChapterOptions(10));
            _mockRepo.Setup(r => r.CreateBlueprintAsync(
                    It.IsAny<ExamBlueprint>(),
                    It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = 2,
                    Status = ExamBlueprintStatus.Approved,
                    UpdatedAtUtc = DateTime.UtcNow
                });

            // Act
            var result = await _service.CreateBlueprintAsync(currentUserId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.ExamBlueprintId);
            Assert.Equal(ExamBlueprintStatus.Approved, result.Status);
            Assert.Equal("Tạo và xuất bản ma trận đề thành công.", result.Message);
            Assert.Empty(result.Warnings);
        }

        // UTCID07 - Normal: Vượt quá số câu hỏi trong ngân hàng -> Có warning nhưng vẫn cho phép tạo
        [Fact]
        public async Task CreateBlueprintAsync_UTCID07_ExceedQuestionBank_ShouldCreateWithWarning()
        {
            // Arrange
            int currentUserId = 1;
            // NotStarted để chỉ sinh cảnh báo khi vượt ngân hàng
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.NotStarted,
                targetTotalQuestions: 5,
                totalQuestionsInRow: 5);

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId))
                     .ReturnsAsync(true);
            // Availability chỉ có 3 câu, ít hơn số yêu cầu 5
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                     .ReturnsAsync(BuildChapterOptions(3));
            _mockRepo.Setup(r => r.CreateBlueprintAsync(
                    It.IsAny<ExamBlueprint>(),
                    It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = 3,
                    Status = ExamBlueprintStatus.NotStarted,
                    UpdatedAtUtc = DateTime.UtcNow
                });

            // Act
            var result = await _service.CreateBlueprintAsync(currentUserId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.ExamBlueprintId);
            Assert.Equal(ExamBlueprintStatus.NotStarted, result.Status);
            Assert.Equal("Lưu nháp ma trận đề thành công.", result.Message);
            Assert.NotEmpty(result.Warnings);
            Assert.Contains(result.Warnings, w => w.Code == "INSUFFICIENT_QUESTION_BANK");
        }
    }
}
