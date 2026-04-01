using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs.StudentExam;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backend_UnitTest.StudentExamTests
{
    public class TakeExamInClass_UTCID_Tests
    {
        private readonly Mock<IStudentExamRepository> _repoMock;
        private readonly Mock<ILogger<StudentExamService>> _loggerMock;
        private readonly StudentExamService _service;

        public TakeExamInClass_UTCID_Tests()
        {
            _repoMock = new Mock<IStudentExamRepository>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<StudentExamService>>();
            _service = new StudentExamService(_repoMock.Object, _loggerMock.Object);
        }

        [Fact(DisplayName = "TakeExamInClass - UTCID01 - Có active submission cùng exam -> tiếp tục làm bài")]
        public async Task TakeExamInClass_UTCID01_HasActiveSubmissionSameExam_ShouldReturnTakeExamDto()
        {
            // Arrange
            int examId = 1;
            int studentId = 1001;

            var examInfo = new ExamInfoForStudentDto
            {
                ExamId = examId,
                Title = "Quiz 1",
                Duration = 60,
                MaxAttempts = 3,
                StudentAttempts = 1,
                CloseAt = DateTime.UtcNow.AddHours(1),
                ShuffleQuestion = false,
                PaperIds = new List<int> { 10 }
            };

            var activeSubmission = new Submission
            {
                SubmissionId = 500,
                StudentId = studentId,
                PaperId = 10,
                Status = SubmissionStatus.InProgress,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
                UpdatedAtUtc = DateTime.UtcNow,
                ConcurrencyStamp = Array.Empty<byte>(),
                Paper = new Paper
                {
                    PaperId = 10,
                    ExamId = examId,
                    Code = 1,
                    Exam = new Exam
                    {
                        ExamId = examId,
                        Duration = 60,
                        ShuffleQuestion = false,
                        TeacherId = 1,
                        Title = "Quiz 1",
                        SubjectId = 1,
                        ShowScore = 1,
                        ShowAnswer = 1,
                        MaxAttempts = 3,
                        AnswerTimingMode = 0,
                        Status = 0,
                        UpdatedAtUtc = DateTime.UtcNow,
                        ConcurrencyStamp = Array.Empty<byte>()
                    }
                }
            };

            var paper = BuildPaperForTakeExam(examId, 10, shuffleQuestion: false);

            _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId))
                .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId))
                .ReturnsAsync(examInfo);
            _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId))
                .ReturnsAsync(activeSubmission);
            _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(examId, activeSubmission.PaperId))
                .ReturnsAsync(paper);

            // Act
            var result = await _service.TakeExamInClass(examId, studentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(examId.ToString(), result!.ExamId);
            Assert.Equal("500", result.SubmissionId);
            Assert.Equal(60, result.Duration);
            Assert.Equal(1, result.Code);
            Assert.NotEmpty(result.Questions);
            Assert.Equal(2, result.Questions.Count);

            _repoMock.Verify(r => r.CreateSubmissionAsync(It.IsAny<Submission>()), Times.Never);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "TakeExamInClass - UTCID02 - ExamInfo null -> return null")]
        public async Task TakeExamInClass_UTCID02_ExamInfoNull_ShouldReturnNull()
        {
            // Arrange
            int examId = 1;
            int studentId = 1001;

            _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId))
                .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId))
                .ReturnsAsync((ExamInfoForStudentDto?)null);

            // Act
            var result = await _service.TakeExamInClass(examId, studentId);

            // Assert
            Assert.Null(result);
            _repoMock.Verify(r => r.GetAnyActiveSubmissionAsync(It.IsAny<int>()), Times.Never);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "TakeExamInClass - UTCID03 - Active submission của exam khác -> throw InvalidOperationException")]
        public async Task TakeExamInClass_UTCID03_ActiveSubmissionOtherExam_ShouldThrowInvalidOperationException()
        {
            // Arrange
            int examId = 1;
            int studentId = 1001;

            var examInfo = new ExamInfoForStudentDto
            {
                ExamId = examId,
                Title = "Quiz 1",
                Duration = 60,
                MaxAttempts = 3,
                StudentAttempts = 1,
                CloseAt = DateTime.UtcNow.AddHours(1),
                ShuffleQuestion = false,
                PaperIds = new List<int> { 10 }
            };

            var activeSubmissionOtherExam = new Submission
            {
                SubmissionId = 501,
                StudentId = studentId,
                PaperId = 99,
                Status = SubmissionStatus.InProgress,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
                UpdatedAtUtc = DateTime.UtcNow,
                ConcurrencyStamp = Array.Empty<byte>(),
                Paper = new Paper
                {
                    PaperId = 99,
                    ExamId = 999,
                    Code = 1
                }
            };

            _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId))
                .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId))
                .ReturnsAsync(examInfo);
            _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId))
                .ReturnsAsync(activeSubmissionOtherExam);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.TakeExamInClass(examId, studentId));

            // Assert
            Assert.Equal("Bạn đang có bài thi khác chưa nộp. Vui lòng hoàn thành hoặc nộp bài đó trước khi bắt đầu bài thi mới.", ex.Message);
            _repoMock.Verify(r => r.CreateSubmissionAsync(It.IsAny<Submission>()), Times.Never);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "TakeExamInClass - UTCID04 - Hết lượt làm bài -> throw InvalidOperationException")]
        public async Task TakeExamInClass_UTCID04_ExceedMaxAttempts_ShouldThrowInvalidOperationException()
        {
            // Arrange
            int examId = 1;
            int studentId = 1001;

            var examInfo = new ExamInfoForStudentDto
            {
                ExamId = examId,
                Title = "Quiz 1",
                Duration = 60,
                MaxAttempts = 1,
                StudentAttempts = 1,
                CloseAt = DateTime.UtcNow.AddHours(1),
                ShuffleQuestion = false,
                PaperIds = new List<int> { 10 }
            };

            _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId))
                .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId))
                .ReturnsAsync(examInfo);
            _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId))
                .ReturnsAsync((Submission?)null);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.TakeExamInClass(examId, studentId));

            // Assert
            Assert.Equal("Bạn đã hết lượt làm bài cho bài thi này.", ex.Message);
            _repoMock.Verify(r => r.CreateSubmissionAsync(It.IsAny<Submission>()), Times.Never);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "TakeExamInClass - UTCID05 - Không có paperIds -> throw InvalidOperationException")]
        public async Task TakeExamInClass_UTCID05_NoPaperIds_ShouldThrowInvalidOperationException()
        {
            // Arrange
            int examId = 1;
            int studentId = 1001;

            var examInfo = new ExamInfoForStudentDto
            {
                ExamId = examId,
                Title = "Quiz 1",
                Duration = 60,
                MaxAttempts = 3,
                StudentAttempts = 0,
                CloseAt = DateTime.UtcNow.AddHours(1),
                ShuffleQuestion = false,
                PaperIds = new List<int>()
            };

            _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId))
                .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId))
                .ReturnsAsync(examInfo);
            _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId))
                .ReturnsAsync((Submission?)null);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.TakeExamInClass(examId, studentId));

            // Assert
            Assert.Equal("Không tìm thấy đề thi nào cho bài kiểm tra này.", ex.Message);
            _repoMock.Verify(r => r.CreateSubmissionAsync(It.IsAny<Submission>()), Times.Never);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "TakeExamInClass - UTCID06 - Không có active submission -> tạo submission mới và trả DTO")]
        public async Task TakeExamInClass_UTCID06_CreateNewSubmission_ShouldReturnTakeExamDto()
        {
            // Arrange
            int examId = 1;
            int studentId = 1001;

            var examInfo = new ExamInfoForStudentDto
            {
                ExamId = examId,
                Title = "Quiz 1",
                Duration = 60,
                MaxAttempts = 3,
                StudentAttempts = 0,
                CloseAt = DateTime.UtcNow.AddHours(1),
                ShuffleQuestion = false,
                PaperIds = new List<int> { 10 } // 1 paper to avoid randomness issue
            };

            var createdSubmission = new Submission
            {
                SubmissionId = 600,
                StudentId = studentId,
                PaperId = 10,
                Status = SubmissionStatus.InProgress,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var paper = BuildPaperForTakeExam(examId, 10, shuffleQuestion: false);

            _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId))
                .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId))
                .ReturnsAsync(examInfo);
            _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId))
                .ReturnsAsync((Submission?)null);
            _repoMock.Setup(r => r.CreateSubmissionAsync(It.Is<Submission>(s =>
                    s.StudentId == studentId &&
                    s.PaperId == 10 &&
                    s.Status == SubmissionStatus.InProgress)))
                .ReturnsAsync(createdSubmission);
            _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(examId, 10))
                .ReturnsAsync(paper);

            // Act
            var result = await _service.TakeExamInClass(examId, studentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("600", result!.SubmissionId);
            Assert.Equal("1", result.ExamId);
            Assert.Equal(2, result.Questions.Count);
            _repoMock.Verify(r => r.CreateSubmissionAsync(It.IsAny<Submission>()), Times.Once);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "TakeExamInClass - UTCID07 - GetPaperWithQuestionsAsync trả null -> return null")]
        public async Task TakeExamInClass_UTCID07_PaperNull_ShouldReturnNull()
        {
            // Arrange
            int examId = 1;
            int studentId = 1001;

            var examInfo = new ExamInfoForStudentDto
            {
                ExamId = examId,
                Title = "Quiz 1",
                Duration = 60,
                MaxAttempts = 3,
                StudentAttempts = 0,
                CloseAt = DateTime.UtcNow.AddHours(1),
                ShuffleQuestion = false,
                PaperIds = new List<int> { 10 }
            };

            var createdSubmission = new Submission
            {
                SubmissionId = 601,
                StudentId = studentId,
                PaperId = 10,
                Status = SubmissionStatus.InProgress,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                ConcurrencyStamp = Array.Empty<byte>()
            };

            _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId))
                .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId))
                .ReturnsAsync(examInfo);
            _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId))
                .ReturnsAsync((Submission?)null);
            _repoMock.Setup(r => r.CreateSubmissionAsync(It.IsAny<Submission>()))
                .ReturnsAsync(createdSubmission);
            _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(examId, 10))
                .ReturnsAsync((Paper?)null);

            // Act
            var result = await _service.TakeExamInClass(examId, studentId);

            // Assert
            Assert.Null(result);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "TakeExamInClass - UTCID08 - ShuffleQuestion = true -> vẫn trả đủ câu hỏi")]
        public async Task TakeExamInClass_UTCID08_ShuffleQuestionTrue_ShouldReturnAllQuestions()
        {
            // Arrange
            int examId = 1;
            int studentId = 1001;

            var examInfo = new ExamInfoForStudentDto
            {
                ExamId = examId,
                Title = "Quiz 1",
                Duration = 60,
                MaxAttempts = 3,
                StudentAttempts = 0,
                CloseAt = DateTime.UtcNow.AddHours(1),
                ShuffleQuestion = true,
                PaperIds = new List<int> { 10 }
            };

            var createdSubmission = new Submission
            {
                SubmissionId = 700,
                StudentId = studentId,
                PaperId = 10,
                Status = SubmissionStatus.InProgress,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var paper = BuildPaperForTakeExam(examId, 10, shuffleQuestion: true);

            _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId))
                .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId))
                .ReturnsAsync(examInfo);
            _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId))
                .ReturnsAsync((Submission?)null);
            _repoMock.Setup(r => r.CreateSubmissionAsync(It.IsAny<Submission>()))
                .ReturnsAsync(createdSubmission);
            _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(examId, 10))
                .ReturnsAsync(paper);

            // Act
            var result = await _service.TakeExamInClass(examId, studentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result!.Questions.Count);
            Assert.Contains(result.Questions, q => q.QuestionId == "101");
            Assert.Contains(result.Questions, q => q.QuestionId == "102");
            _repoMock.VerifyAll();
        }

        private static Paper BuildPaperForTakeExam(int examId, int paperId, bool shuffleQuestion)
        {
            var inputType = new InputType
            {
                InputTypeId = 1,
                Name = "Text",
                Regex = ".*",
                GroupType = "single"
            };

            var q1 = new Question
            {
                QuestionId = 101,
                QuestionType = "MCQ",
                QuestionContent = "Question 1",
                Difficulty = 1,
                CreatedByUserId = 1,
                UpdatedAtUtc = DateTime.UtcNow,
                Status = "Active",
                ConcurrencyStamp = Array.Empty<byte>(),
                ChapterId = 1,
                CreatedByUser = new User
                {
                    UserId = 1,
                    Email = "teacher@example.com",
                    ConcurrencyStamp = Array.Empty<byte>()
                },
                Chapter = new Chapter
                {
                    ChapterId = 1,
                    SubjectId = 1,
                    Name = "Chapter 1"
                },
                QuestionAnswers = new List<QuestionAnswer>
                {
                    new QuestionAnswer
                    {
                        QuestionAnswerId = 1001,
                        QuestionId = 101,
                        Content = "Answer 1",
                        CorrectAnswer = "A",
                        GroupAnswerId = 1,
                        IsCorrect = true,
                        ConcurrencyStamp = Array.Empty<byte>(),
                        BlankInputs = new List<BlankInput>
                        {
                            new BlankInput
                            {
                                QuestionAnswerId = 1001,
                                InputTypeId = 1,
                                ConcurrencyStamp = Array.Empty<byte>(),
                                InputType = inputType
                            }
                        }
                    }
                }
            };

            var q2 = new Question
            {
                QuestionId = 102,
                QuestionType = "MCQ",
                QuestionContent = "Question 2",
                Difficulty = 2,
                CreatedByUserId = 1,
                UpdatedAtUtc = DateTime.UtcNow,
                Status = "Active",
                ConcurrencyStamp = Array.Empty<byte>(),
                ChapterId = 1,
                CreatedByUser = new User
                {
                    UserId = 1,
                    Email = "teacher@example.com",
                    ConcurrencyStamp = Array.Empty<byte>()
                },
                Chapter = new Chapter
                {
                    ChapterId = 1,
                    SubjectId = 1,
                    Name = "Chapter 1"
                },
                QuestionAnswers = new List<QuestionAnswer>
                {
                    new QuestionAnswer
                    {
                        QuestionAnswerId = 1002,
                        QuestionId = 102,
                        Content = "Answer 2",
                        CorrectAnswer = "B",
                        GroupAnswerId = null,
                        IsCorrect = false,
                        ConcurrencyStamp = Array.Empty<byte>(),
                        BlankInputs = new List<BlankInput>()
                    }
                }
            };

            return new Paper
            {
                PaperId = paperId,
                ExamId = examId,
                Code = 1,
                Exam = new Exam
                {
                    ExamId = examId,
                    Duration = 60,
                    ShuffleQuestion = shuffleQuestion,
                    TeacherId = 1,
                    Title = "Quiz 1",
                    SubjectId = 1,
                    ShowScore = 1,
                    ShowAnswer = 1,
                    MaxAttempts = 3,
                    AnswerTimingMode = 0,
                    Status = 0,
                    UpdatedAtUtc = DateTime.UtcNow,
                    ConcurrencyStamp = Array.Empty<byte>()
                },
                Questions = new List<Question> { q1, q2 }
            };
        }
    }
}