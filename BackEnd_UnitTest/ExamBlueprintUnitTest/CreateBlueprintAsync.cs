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
using System.Reflection;
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
            int totalQuestionsInRow = 5,
            int subjectId = 8,
            string? name = "Blueprint 1",
            string? description = "Description",
            List<CreateExamBlueprintRowDto>? rows = null)
        {
            rows ??= new List<CreateExamBlueprintRowDto>
            {
                new CreateExamBlueprintRowDto
                {
                    ChapterId = 1,
                    Difficulty = 1,
                    TotalQuestions = totalQuestionsInRow
                }
            };

            return new CreateExamBlueprintRequest
            {
                Name = name ?? string.Empty,
                Description = description,
                SubjectId = subjectId,
                TargetStatus = targetStatus,
                TargetTotalQuestions = targetTotalQuestions,
                Rows = rows
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

        private static List<ChapterOptionDto> BuildChapterOptionsForDifficulty(
            Dictionary<int, int> availabilityByDifficulty)
        {
            return new List<ChapterOptionDto>
            {
                new ChapterOptionDto
                {
                    ChapterId = 1,
                    Name = "Chương 1",
                    AvailabilityByDifficulty = availabilityByDifficulty
                        .Select(kvp => new ChapterAvailabilityDto
                        {
                            Difficulty = kvp.Key,
                            AvailableQuestions = kvp.Value
                        }).ToList()
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

        // UTCID08 - Normal: Name > 200 ký tự -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID08_NameTooLong_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                name: new string('a', 201));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            Assert.Contains("Tên ma trận đề không được vượt quá 200 ký tự.", exception.Errors);
            _mockRepo.Verify(r => r.SubjectExistsAsync(It.IsAny<int>()), Times.Never);
            _mockRepo.Verify(r => r.GetChaptersBySubjectAsync(It.IsAny<int>()), Times.Never);
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID09 - Normal: Description > 1000 ký tự -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID09_DescriptionTooLong_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                description: new string('b', 1001));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            Assert.Contains("Mô tả không được vượt quá 1000 ký tự.", exception.Errors);
            _mockRepo.Verify(r => r.SubjectExistsAsync(It.IsAny<int>()), Times.Never);
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID10 - Normal: SubjectId <= 0 -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID10_SubjectIdInvalid_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(subjectId: 0);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            Assert.Contains("Môn học không hợp lệ.", exception.Errors);
            _mockRepo.Verify(r => r.SubjectExistsAsync(It.IsAny<int>()), Times.Never);
        }

        // UTCID11 - Normal: TargetStatus invalid -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID11_TargetStatusInvalid_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(targetStatus: 99);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            Assert.Contains("Trạng thái mục tiêu không hợp lệ.", exception.Errors);
            _mockRepo.Verify(r => r.SubjectExistsAsync(It.IsAny<int>()), Times.Never);
        }

        // UTCID12 - Normal: TargetTotalQuestions < 0 -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID12_TargetTotalQuestionsNegative_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(targetTotalQuestions: -1);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            Assert.Contains("Tổng số câu mục tiêu không được âm.", exception.Errors);
            _mockRepo.Verify(r => r.SubjectExistsAsync(It.IsAny<int>()), Times.Never);
        }

        // UTCID13 - Normal: Approved + Rows empty -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID13_Approved_RowsEmpty_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.Approved,
                targetTotalQuestions: 1,
                rows: new List<CreateExamBlueprintRowDto>());

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            Assert.Contains("Xuất bản yêu cầu ít nhất một dòng ma trận.", exception.Errors);
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID14 - Normal: Approved + TargetTotalQuestions <= 0 & row total <= 0 -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID14_Approved_TargetTotalQuestionsAndRowTotalNonPositive_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.Approved,
                targetTotalQuestions: 0,
                totalQuestionsInRow: 0);

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            Assert.Contains("Xuất bản yêu cầu tổng số câu mục tiêu lớn hơn 0.", exception.Errors);
            Assert.Contains("Xuất bản yêu cầu mỗi dòng ma trận có số câu lớn hơn 0.", exception.Errors);
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID15 - Normal: Approved + TargetTotalQuestions mismatch rowTotal -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID15_Approved_TargetTotalMismatch_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.Approved,
                targetTotalQuestions: 6,
                totalQuestionsInRow: 5);

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            Assert.Contains("Tổng số câu mục tiêu phải bằng tổng số câu của các dòng ma trận.", exception.Errors);
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID16 - Normal: Approved + exceed question bank -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID16_Approved_ExceedQuestionBank_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.Approved,
                targetTotalQuestions: 5,
                totalQuestionsInRow: 5);

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            // bank only has 3, request wants 5 -> insufficient error
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(3));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            Assert.Contains(exception.Errors, e => e.Contains("Số câu vượt ngân hàng câu hỏi"));
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID17 - Normal: NotStarted + targetTotal mismatch rowTotal -> TARGET_TOTAL_MISMATCH warning (still create)
        [Fact]
        public async Task CreateBlueprintAsync_UTCID17_NotStarted_TargetTotalMismatch_ShouldCreateWithTargetTotalMismatchWarning()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.NotStarted,
                targetTotalQuestions: 6,
                totalQuestionsInRow: 5);

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));
            _mockRepo.Setup(r => r.CreateBlueprintAsync(
                    It.IsAny<ExamBlueprint>(),
                    It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = 4,
                    Status = ExamBlueprintStatus.NotStarted,
                    UpdatedAtUtc = DateTime.UtcNow
                });

            // Act
            var result = await _service.CreateBlueprintAsync(currentUserId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Warnings, w => w.Code == "TARGET_TOTAL_MISMATCH");
        }

        // UTCID18 - Normal: NotStarted + availability missing (TryGetValue false) -> INSUFFICIENT_QUESTION_BANK warning
        [Fact]
        public async Task CreateBlueprintAsync_UTCID18_NotStarted_AvailabilityMissingTryGetValueFalse_ShouldCreateWithInsufficientWarning()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.NotStarted,
                targetTotalQuestions: 5,
                totalQuestionsInRow: 5,
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 2, TotalQuestions = 5 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            // only difficulty = 1 exists, difficulty = 2 missing => available = 0
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10)); // difficulty 1 only
            _mockRepo.Setup(r => r.CreateBlueprintAsync(
                    It.IsAny<ExamBlueprint>(),
                    It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = 5,
                    Status = ExamBlueprintStatus.NotStarted,
                    UpdatedAtUtc = DateTime.UtcNow
                });

            // Act
            var result = await _service.CreateBlueprintAsync(currentUserId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result.Warnings, w => w.Code == "INSUFFICIENT_QUESTION_BANK");
        }

        // UTCID19 - Normal: UpdatedAtUtc == default -> response uses 'now'
        [Fact]
        public async Task CreateBlueprintAsync_UTCID19_UpdatedAtUtcDefault_ShouldUseNow()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest();

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));
            _mockRepo.Setup(r => r.CreateBlueprintAsync(
                    It.IsAny<ExamBlueprint>(),
                    It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = 6,
                    Status = ExamBlueprintStatus.Approved,
                    UpdatedAtUtc = default
                });

            var before = DateTime.UtcNow;

            // Act
            var result = await _service.CreateBlueprintAsync(currentUserId, request);
            var after = DateTime.UtcNow;

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(default, result.UpdatedAtUtc);
            Assert.InRange(result.UpdatedAtUtc, before, after);
        }

        // UTCID20 - Normal: Duplicate rows -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID20_DuplicateRows_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 1, TotalQuestions = 5 },
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 1, TotalQuestions = 5 }
                },
                targetTotalQuestions: 10);

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            Assert.Contains(exception.Errors, e => e.Contains("Trùng dòng ma trận"));
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID21 - Normal: ChapterId > 0 nhưng không thuộc môn học đã chọn -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID21_RowChapterNotBelongToSubject_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.Approved,
                targetTotalQuestions: 5,
                totalQuestionsInRow: 5,
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 999, Difficulty = 1, TotalQuestions = 5 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            // DB chỉ có ChapterId = 1, không có 999
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            // Assert
            Assert.Contains(exception.Errors, e => e.Contains("không thuộc môn học đã chọn"));
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID22 - Normal: Difficulty ngoài 1..4 -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID22_RowDifficultyInvalid_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.Approved,
                targetTotalQuestions: 5,
                totalQuestionsInRow: 5,
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 0, TotalQuestions = 5 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            // Assert
            Assert.Contains(exception.Errors, e => e.Contains("không hợp lệ"));
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID23 - Normal: TotalQuestions < 0 trong dòng -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID23_RowTotalQuestionsNegative_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            // NotStarted để tránh thêm lỗi Approved về tổng số câu mục tiêu, nhưng errors vẫn do row.TotalQuestions < 0
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.NotStarted,
                targetTotalQuestions: 5,
                totalQuestionsInRow: -1,
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 1, TotalQuestions = -1 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            // Assert
            Assert.Contains(exception.Errors, e => e.Contains("không được âm"));
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID24 - Normal: Description null/empty -> không đánh giá description.Length > 1000
        [Fact]
        public async Task CreateBlueprintAsync_UTCID24_DescriptionNullOrEmpty_ShouldCreateSuccessfully()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.Approved,
                targetTotalQuestions: 5,
                totalQuestionsInRow: 5,
                description: "");

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));
            _mockRepo.Setup(r => r.CreateBlueprintAsync(
                    It.IsAny<ExamBlueprint>(),
                    It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = 7,
                    Status = ExamBlueprintStatus.Approved,
                    UpdatedAtUtc = DateTime.UtcNow
                });

            // Act
            var result = await _service.CreateBlueprintAsync(currentUserId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Warnings);
        }

        // UTCID25 - Normal: Difficulty > 4 -> ExamBlueprintValidationException
        [Fact]
        public async Task CreateBlueprintAsync_UTCID25_RowDifficultyGreaterThanFour_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.Approved,
                targetTotalQuestions: 5,
                totalQuestionsInRow: 5,
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 5, TotalQuestions = 5 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            // Assert
            Assert.Contains(exception.Errors, e => e.Contains("không hợp lệ"));
            _mockRepo.Verify(r => r.CreateBlueprintAsync(
                It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()), Times.Never);
        }

        // UTCID26 - Normal: Cover GetDifficultyLabel cases 3 & 4 via insufficient bank (NotStarted)
        [Fact]
        public async Task CreateBlueprintAsync_UTCID26_InsufficientBankForDifficulty3And4_ShouldCreateWithWarnings()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.NotStarted,
                targetTotalQuestions: 10,
                totalQuestionsInRow: 5,
                rows: new List<CreateExamBlueprintRowDto>
                {
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 3, TotalQuestions = 5 },
                    new CreateExamBlueprintRowDto { ChapterId = 1, Difficulty = 4, TotalQuestions = 5 }
                });

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            // BuildChapterOptions chỉ có difficulty = 1 -> difficulty 3/4 => available = 0
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));
            _mockRepo.Setup(r => r.CreateBlueprintAsync(
                    It.IsAny<ExamBlueprint>(),
                    It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = 8,
                    Status = ExamBlueprintStatus.NotStarted,
                    UpdatedAtUtc = DateTime.UtcNow
                });

            // Act
            var result = await _service.CreateBlueprintAsync(currentUserId, request);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Warnings);
            Assert.Contains(result.Warnings,
                w => w.Code == "INSUFFICIENT_QUESTION_BANK" && w.Message.Contains("Vận dụng"));
            Assert.Contains(result.Warnings,
                w => w.Code == "INSUFFICIENT_QUESTION_BANK" && w.Message.Contains("Vận dụng cao"));
        }

        // UTCID27 - Normal: Cover default case của GetDifficultyLabel bằng reflection
        [Fact]
        public void CreateBlueprintAsync_UTCID27_GetDifficultyLabel_DefaultCase_ShouldReturnDifficultyToString()
        {
            // Arrange
            var method = typeof(ExamBlueprintService).GetMethod(
                "GetDifficultyLabel",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = (string?)method!.Invoke(null, new object[] { 0 });

            // Assert
            Assert.Equal("0", result);
        }

        // UTCID28 - Normal: request.Name = null -> cover nhánh (request.Name ?? string.Empty)
        [Fact]
        public async Task CreateBlueprintAsync_UTCID28_NameNull_ShouldThrowExamBlueprintValidationException()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.Approved,
                targetTotalQuestions: 5,
                totalQuestionsInRow: 5);
            request.Name = null!;

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));

            // Act
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.CreateBlueprintAsync(currentUserId, request));

            // Assert
            Assert.Contains("Tên ma trận đề là bắt buộc.", exception.Errors);
        }

        // UTCID29 - Normal: request.Rows = null -> cover nhánh (request.Rows ?? new List<...>())
        [Fact]
        public async Task CreateBlueprintAsync_UTCID29_RowsNull_NotStarted_ShouldCreateSuccessfully()
        {
            // Arrange
            int currentUserId = 1;
            var request = BuildValidRequest(
                targetStatus: ExamBlueprintStatus.NotStarted,
                targetTotalQuestions: 0,
                totalQuestionsInRow: 0);
            request.Rows = null!;

            _mockRepo.Setup(r => r.SubjectExistsAsync(request.SubjectId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(request.SubjectId))
                .ReturnsAsync(BuildChapterOptions(10));
            _mockRepo.Setup(r => r.CreateBlueprintAsync(
                    It.IsAny<ExamBlueprint>(),
                    It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = 9,
                    Status = ExamBlueprintStatus.NotStarted,
                    UpdatedAtUtc = DateTime.UtcNow
                });

            // Act
            var result = await _service.CreateBlueprintAsync(currentUserId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Warnings);
            Assert.Equal(ExamBlueprintStatus.NotStarted, result.Status);
        }
    }
}
