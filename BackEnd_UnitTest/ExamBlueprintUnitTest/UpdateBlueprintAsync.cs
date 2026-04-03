using Backend.Constants;
using Backend.DTOs.ExamBlueprint;
using Backend.Exceptions;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private static CreateExamBlueprintRequest BuildValidRequest(
            int targetStatus = ExamBlueprintStatus.Approved,
            int targetTotal = 5,
            int subjectId = 8,
            string? name = "Blueprint update",
            string? description = "Description",
            List<CreateExamBlueprintRowDto>? rows = null)
        {
            return new CreateExamBlueprintRequest
            {
                Name = name ?? string.Empty,
                Description = description,
                SubjectId = subjectId,
                TargetStatus = targetStatus,
                TargetTotalQuestions = targetTotal,
                Rows = rows ?? new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 1, TotalQuestions = targetTotal }
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
            // tạo mismatch: TargetTotalQuestions = 10 nhưng tổng row = 5
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.NotStarted,
                targetTotal: 10,
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 1, TotalQuestions = 5 }
                });

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

        // UTCID07 - Normal: Name > 200 -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID07_NameTooLong_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(name: new string('a', 201));

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains("Tên ma trận đề không được vượt quá 200 ký tự.", exception.Errors);
            _mockRepo.Verify(r => r.UpdateBlueprintAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<ExamBlueprint>(),
                It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID08 - Normal: Description > 1000 -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID08_DescriptionTooLong_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(description: new string('b', 1001));

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains("Mô tả không được vượt quá 1000 ký tự.", exception.Errors);
            _mockRepo.Verify(r => r.UpdateBlueprintAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<ExamBlueprint>(),
                It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID09 - Normal: SubjectId <= 0 -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID09_SubjectIdInvalid_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(subjectId: 0);

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains("Môn học không hợp lệ.", exception.Errors);
        }

        // UTCID10 - Normal: TargetStatus invalid -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID10_TargetStatusInvalid_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(targetStatus: 99);

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains("Trạng thái mục tiêu không hợp lệ.", exception.Errors);
        }

        // UTCID11 - Normal: TargetTotalQuestions < 0 -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID11_TargetTotalQuestionsNegative_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(targetTotal: -1);

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains("Tổng số câu mục tiêu không được âm.", exception.Errors);
        }

        // UTCID12 - Normal: Subject not found -> KeyNotFoundException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID12_SubjectNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(subjectId: 123);

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(false);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Equal("Không tìm thấy môn học.", exception.Message);
            _mockRepo.Verify(r => r.GetChaptersBySubjectAsync(It.IsAny<int>()), Times.Never);
        }

        // UTCID13 - Normal: Row invalid ChapterId (<=0) -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID13_RowInvalidChapterId_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 0, Difficulty = 1, TotalQuestions = 5 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains("Mỗi dòng ma trận phải có chương hợp lệ.", exception.Errors);
        }

        // UTCID14 - Normal: Row invalid Difficulty -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID14_RowInvalidDifficulty_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 5, TotalQuestions = 5 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains("Mức độ 5 không hợp lệ.", exception.Errors);
        }

        // UTCID15 - Normal: Row negative TotalQuestions -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID15_RowNegativeTotalQuestions_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetTotal: 0,
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 1, TotalQuestions = -1 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains("Số câu trong từng dòng không được âm.", exception.Errors);
        }

        // UTCID16 - Normal: Duplicate rows (same ChapterId + Difficulty) -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID16_DuplicateRows_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetTotal: 10,
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 1, TotalQuestions = 5 },
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 1, TotalQuestions = 5 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains(exception.Errors, e => e.Contains("Trùng dòng ma trận"));
        }

        // UTCID17 - Normal: Approved + exceed question bank -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID17_ApprovedExceedQuestionBank_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.Approved,
                targetTotal: 5,
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 1, TotalQuestions = 5 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(3)); // available < requested

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains(exception.Errors, e => e.Contains("Số câu vượt ngân hàng câu hỏi"));
        }

        // UTCID18 - Normal: NotStarted + exceed question bank -> warning INSUFFICIENT_QUESTION_BANK (still save)
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID18_NotStartedExceedQuestionBank_ShouldCreateWithWarning()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.NotStarted,
                targetTotal: 5,
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 1, TotalQuestions = 5 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(3)); // available < requested

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
            Assert.Single(result.Warnings, w => w.Code == "INSUFFICIENT_QUESTION_BANK");
        }

        // UTCID19 - Normal: Row chapterId not in subject chapters -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID19_RowChapterNotInSubject_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 999, Difficulty = 1, TotalQuestions = 5 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10)); // chỉ có chapterId = 1

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains(exception.Errors, e => e.Contains("không thuộc môn học đã chọn"));
            _mockRepo.Verify(r => r.UpdateBlueprintAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<ExamBlueprint>(),
                It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID20 - Normal: availabilityMap.TryGetValue = false (difficulty có nhưng không có trong availability) + Approved -> lỗi
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID20_MissingAvailabilityForDifficulty_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.Approved,
                targetTotal: 5,
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 2, TotalQuestions = 5 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            // BuildChapterOptions chỉ tạo availability cho Difficulty = 1
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(3));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains(exception.Errors, e => e.Contains("Số câu vượt ngân hàng câu hỏi"));
            Assert.Contains(exception.Errors, e => e.Contains("hiện có 0"));
        }

        // UTCID21 - Normal: availabilityMap.TryGetValue = false + NotStarted -> warning INSUFFICIENT_QUESTION_BANK
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID21_MissingAvailabilityForDifficulty_NotStarted_ShouldReturnWarning()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.NotStarted,
                targetTotal: 5,
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 2, TotalQuestions = 5 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(3));

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
            Assert.Single(result.Warnings, w => w.Code == "INSUFFICIENT_QUESTION_BANK");
            Assert.Contains(result.Warnings, w => w.Message.Contains("hiện có 0"));
        }

        // UTCID22 - Normal: request.Rows = null -> sử dụng default empty list (rows.Count = 0) + NotStarted -> vẫn update thành công
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID22_RowsNull_NotStarted_ShouldUpdateSuccessfully()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(targetStatus: ExamBlueprintStatus.NotStarted, targetTotal: 0);
            request.Rows = null!;

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
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
            Assert.Empty(result.Warnings);
        }

        // UTCID23 - Normal: request.Name = null -> lỗi "Tên ma trận đề là bắt buộc."
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID23_NameNull_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest();
            request.Name = null!;

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains("Tên ma trận đề là bắt buộc.", exception.Errors);
        }

        // UTCID24 - Normal: request.Description = null -> không bị lỗi mô tả
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID24_DescriptionNull_ShouldUpdateSuccessfully()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest();
            request.Description = null!;

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
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
            Assert.Empty(result.Warnings);
        }

        // UTCID25 - Normal: row.Difficulty < 1 -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID25_RowDifficultyZero_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.Approved,
                targetTotal: 5,
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 0, TotalQuestions = 5 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains("Mức độ 0 không hợp lệ.", exception.Errors);
        }

        // UTCID26 - Normal: Approved + rows empty -> ExamBlueprintValidationException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID26_ApprovedRowsEmpty_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int blueprintId = 1;
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.Approved,
                targetTotal: 1,
                rows: new List<CreateExamBlueprintRowDto>());

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Contains("Xuất bản yêu cầu ít nhất một dòng ma trận.", exception.Errors);
        }

        // UTCID27 - Normal: id <= 0 nhưng currentUserId hợp lệ -> UnauthorizedAccessException
        [Fact]
        public async Task UpdateBlueprintAsync_UTCID27_BlueprintIdIsZero_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            int blueprintId = 0;
            int currentUserId = 1;
            var request = BuildValidRequest();

            // Act
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.UpdateBlueprintAsync(blueprintId, currentUserId, request));

            // Assert
            Assert.Equal("Invalid user or blueprint id.", exception.Message);
            _mockRepo.Verify(r => r.UpdateBlueprintAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<ExamBlueprint>(),
                    It.IsAny<IEnumerable<ExamBlueprintChapter>>()),
                Times.Never);
        }
    }
}
