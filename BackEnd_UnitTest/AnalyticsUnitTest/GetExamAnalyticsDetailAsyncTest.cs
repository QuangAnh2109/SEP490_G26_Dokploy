using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using Xunit;

namespace Backend_UnitTest.AnalyticsTests
{
    public class GetExamAnalyticsDetailAsync_UTCID_Tests
    {
        private readonly Mock<IAnalyticsRepository> _analyticsRepoMock;
        private readonly Mock<IStudentExamRepository> _studentExamRepoMock;
        private readonly AnalyticsService _service;

        public GetExamAnalyticsDetailAsync_UTCID_Tests()
        {
            _analyticsRepoMock = new Mock<IAnalyticsRepository>(MockBehavior.Strict);
            _studentExamRepoMock = new Mock<IStudentExamRepository>(MockBehavior.Strict);
            _service = new AnalyticsService(_analyticsRepoMock.Object, _studentExamRepoMock.Object, TimeProvider.System);
        }

        [Fact(DisplayName = "GetExamAnalyticsDetailAsync - UTCID01 - Exam tồn tại, có dữ liệu đầy đủ -> trả analytics detail")]
        public async Task GetExamAnalyticsDetailAsync_UTCID01_ExamExists_WithValidData_ShouldReturnAnalyticsDetail()
        {
            // Arrange
            int examId = 1;
            var exam = BuildExamWithValidAnalyticsData(examId);

            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId))
                              .ReturnsAsync(exam);

            // Act
            var result = await _service.GetExamAnalyticsDetailAsync(examId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(examId, result.ExamId);
            Assert.Equal("Midterm Math", result.ExamTitle);

            // rawSubmissions = 2
            Assert.Equal(2, result.TotalSubmissions);

            // scores = [6, 8]
            Assert.Equal(7.00m, result.AverageScore);
            Assert.Equal(8m, result.MaxScore);
            Assert.Equal(6m, result.MinScore);
            Assert.Equal(7.00m, result.MedianScore);

            Assert.NotEmpty(result.ScoreDistribution);
            Assert.Equal(1, result.ScoreDistribution["6-7"]);
            Assert.Equal(1, result.ScoreDistribution["8-9"]);

            Assert.Equal(2, result.ChapterStats.Count);
            Assert.Equal(2, result.DifficultyStats.Count);
            Assert.NotEmpty(result.HardestQuestions);
            Assert.Equal(2, result.StudentResults.Count);
            Assert.NotEmpty(result.Recommendations);
            Assert.NotNull(result.DebugInfo);

            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamAnalyticsDetailAsync - UTCID02 - Exam tồn tại nhưng chưa có submission -> return sớm với recommendation")]
        public async Task GetExamAnalyticsDetailAsync_UTCID02_NoSubmission_ShouldReturnEarlyRecommendation()
        {
            // Arrange
            int examId = 2;
            var exam = new Exam
            {
                ExamId = examId,
                Title = "No Submission Exam",
                Papers = new List<Paper>
                {
                    new Paper
                    {
                        PaperId = 1,
                        ExamId = examId,
                        Code = 1,
                        Questions = new List<Question>(),
                        Submissions = new List<Submission>()
                    }
                },
                ConcurrencyStamp = Array.Empty<byte>(),
                UpdatedAtUtc = DateTime.UtcNow
            };

            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId))
                              .ReturnsAsync(exam);

            // Act
            var result = await _service.GetExamAnalyticsDetailAsync(examId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(examId, result.ExamId);
            Assert.Equal("No Submission Exam", result.ExamTitle);
            Assert.Equal(0, result.TotalSubmissions);
            Assert.Contains("Chưa có học sinh nào nộp bài thi này để phân tích.", result.Recommendations);

            Assert.Empty(result.ChapterStats);
            Assert.Empty(result.DifficultyStats);
            Assert.Empty(result.HardestQuestions);
            Assert.Empty(result.StudentResults);

            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamAnalyticsDetailAsync - UTCID03 - Nhiều submission cùng 1 học sinh -> chỉ lấy submission mới nhất để phân tích")]
        public async Task GetExamAnalyticsDetailAsync_UTCID03_MultipleSubmissionsSameStudent_ShouldUseLatestSubmission()
        {
            // Arrange
            int examId = 3;

            var teacher = new User
            {
                UserId = 99,
                Email = "teacher@x.com",
                FullName = "Teacher A",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var chapter = new Chapter
            {
                ChapterId = 10,
                SubjectId = 1,
                Name = "Chương 1"
            };

            var q1 = CreateQuestion(101, "Q1", chapter, 1);
            var q2 = CreateQuestion(102, "Q2", chapter, 1);

            var student1 = new User
            {
                UserId = 1,
                Email = "s1@x.com",
                FullName = "Student 1",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var student2 = new User
            {
                UserId = 2,
                Email = "s2@x.com",
                FullName = "Student 2",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            // student1 có 2 submissions, service phải lấy submission mới nhất
            var subOld = CreateSubmission(
                submissionId: 1,
                studentId: 1,
                updatedAtUtc: new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc),
                totalPoints: 4m,
                student: student1,
                answers: new List<StudentAnswer>
                {
                    CreateStudentAnswer(1, q1.QuestionAnswers.First(), "A")
                });

            var subNew = CreateSubmission(
                submissionId: 2,
                studentId: 1,
                updatedAtUtc: new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc),
                totalPoints: 9m,
                student: student1,
                answers: new List<StudentAnswer>
                {
                    CreateStudentAnswer(2, q1.QuestionAnswers.First(), "A"),
                    CreateStudentAnswer(3, q2.QuestionAnswers.First(), "A")
                });

            var subStudent2 = CreateSubmission(
                submissionId: 3,
                studentId: 2,
                updatedAtUtc: new DateTime(2026, 4, 1, 10, 30, 0, DateTimeKind.Utc),
                totalPoints: 7m,
                student: student2,
                answers: new List<StudentAnswer>
                {
                    CreateStudentAnswer(4, q1.QuestionAnswers.First(), "A")
                });

            var exam = new Exam
            {
                ExamId = examId,
                Title = "Latest Submission Exam",
                Papers = new List<Paper>
                {
                    new Paper
                    {
                        PaperId = 10,
                        ExamId = examId,
                        Code = 1,
                        Questions = new List<Question> { q1, q2 },
                        Submissions = new List<Submission> { subOld, subNew, subStudent2 }
                    }
                },
                ConcurrencyStamp = Array.Empty<byte>(),
                UpdatedAtUtc = DateTime.UtcNow
            };

            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId))
                              .ReturnsAsync(exam);

            // Act
            var result = await _service.GetExamAnalyticsDetailAsync(examId);

            // Assert
            // raw submissions vẫn là 3
            Assert.Equal(3, result.TotalSubmissions);

            // nhưng StudentResults chỉ còn 2 học sinh
            Assert.Equal(2, result.StudentResults.Count);

            var student1Result = result.StudentResults.Single(x => x.StudentId == 1);
            Assert.Equal(9m, student1Result.TotalPoints);

            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamAnalyticsDetailAsync - UTCID04 - Có submission rỗng -> loại khỏi allSubmissions nhưng vẫn giữ TotalSubmissions")]
        public async Task GetExamAnalyticsDetailAsync_UTCID04_EmptySubmission_ShouldBeExcludedFromAnalytics()
        {
            // Arrange
            int examId = 4;

            var student1 = new User
            {
                UserId = 1,
                Email = "s1@x.com",
                FullName = "Student 1",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var student2 = new User
            {
                UserId = 2,
                Email = "s2@x.com",
                FullName = "Student 2",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var chapter = new Chapter
            {
                ChapterId = 10,
                SubjectId = 1,
                Name = "Chương 1"
            };

            var q1 = CreateQuestion(101, "Q1", chapter, 1);

            var emptySubmission = CreateSubmission(
                submissionId: 1,
                studentId: 1,
                updatedAtUtc: DateTime.UtcNow.AddMinutes(-10),
                totalPoints: 0m,
                student: student1,
                answers: new List<StudentAnswer>()); // empty

            var validSubmission = CreateSubmission(
                submissionId: 2,
                studentId: 2,
                updatedAtUtc: DateTime.UtcNow,
                totalPoints: 8m,
                student: student2,
                answers: new List<StudentAnswer>
                {
                    CreateStudentAnswer(1, q1.QuestionAnswers.First(), "A")
                });

            var exam = new Exam
            {
                ExamId = examId,
                Title = "Exclude Empty Submission",
                Papers = new List<Paper>
                {
                    new Paper
                    {
                        PaperId = 10,
                        ExamId = examId,
                        Code = 1,
                        Questions = new List<Question> { q1 },
                        Submissions = new List<Submission> { emptySubmission, validSubmission }
                    }
                },
                ConcurrencyStamp = Array.Empty<byte>(),
                UpdatedAtUtc = DateTime.UtcNow
            };

            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId))
                              .ReturnsAsync(exam);

            // Act
            var result = await _service.GetExamAnalyticsDetailAsync(examId);

            // Assert
            // raw submissions vẫn là 2
            Assert.Equal(2, result.TotalSubmissions);

            // allSubmissions chỉ còn 1 học sinh hợp lệ
            Assert.Single(result.StudentResults);
            Assert.Equal(2, result.StudentResults[0].StudentId);

            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamAnalyticsDetailAsync - UTCID05 - Thiếu FullName -> fallback Email/HS#")]
        public async Task GetExamAnalyticsDetailAsync_UTCID05_MissingStudentName_ShouldFallbackEmailOrStudentCode()
        {
            // Arrange
            int examId = 5;

            var chapter = new Chapter
            {
                ChapterId = 10,
                SubjectId = 1,
                Name = "Chương 1"
            };

            var q1 = CreateQuestion(101, "Q1", chapter, 1);

            var studentNoFullName = new User
            {
                UserId = 1,
                Email = "fallback@email.com",
                FullName = null,
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var submission = CreateSubmission(
                submissionId: 1,
                studentId: 1,
                updatedAtUtc: DateTime.UtcNow,
                totalPoints: 7m,
                student: studentNoFullName,
                answers: new List<StudentAnswer>
                {
                    CreateStudentAnswer(1, q1.QuestionAnswers.First(), "A")
                });

            var exam = new Exam
            {
                ExamId = examId,
                Title = "Fallback Student Name",
                Papers = new List<Paper>
                {
                    new Paper
                    {
                        PaperId = 10,
                        ExamId = examId,
                        Code = 1,
                        Questions = new List<Question> { q1 },
                        Submissions = new List<Submission> { submission }
                    }
                },
                ConcurrencyStamp = Array.Empty<byte>(),
                UpdatedAtUtc = DateTime.UtcNow
            };

            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId))
                              .ReturnsAsync(exam);

            // Act
            var result = await _service.GetExamAnalyticsDetailAsync(examId);

            // Assert
            Assert.Single(result.StudentResults);
            Assert.Equal("fallback@email.com", result.StudentResults[0].StudentName);

            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamAnalyticsDetailAsync - UTCID06 - Exam không tồn tại -> KeyNotFoundException")]
        public async Task GetExamAnalyticsDetailAsync_UTCID06_ExamNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            int examId = 999;

            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId))
                              .ReturnsAsync((Exam?)null);

            // Act
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetExamAnalyticsDetailAsync(examId));

            // Assert
            Assert.Equal($"Không tìm thấy bài thi với ID {examId}.", ex.Message);
            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamAnalyticsDetailAsync - UTCID07 - Submission null answers, null question nav, null student info -> fallback đúng")]
        public async Task GetExamAnalyticsDetailAsync_UTCID07_NullAnswersAndNullQuestionNavigation_ShouldFallbackCorrectly()
        {
            int examId = 7;

            var chapter = new Chapter
            {
                ChapterId = 10,
                SubjectId = 1,
                Name = "Chương 1"
            };

            var q1 = CreateQuestion(101, "Q1", chapter, 1);
            q1.QuestionAnswers.First().Question = null!;

            var studentNoInfo = new User
            {
                UserId = 1,
                Email = null!,
                FullName = null,
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var nullAnswersSubmission = new Submission
            {
                SubmissionId = 1,
                StudentId = 99,
                PaperId = 10,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-40),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-20),
                TotalPoints = 0m,
                Status = 1,
                ConcurrencyStamp = Array.Empty<byte>(),
                Student = new User
                {
                    UserId = 99,
                    Email = "ignored@x.com",
                    FullName = "Ignored",
                    ConcurrencyStamp = Array.Empty<byte>()
                },
                StudentAnswers = null!
            };

            var validSubmission = CreateSubmission(
                submissionId: 2,
                studentId: 1,
                updatedAtUtc: DateTime.UtcNow,
                totalPoints: 5m,
                student: studentNoInfo,
                answers: new List<StudentAnswer>
                {
                    CreateStudentAnswer(1, q1.QuestionAnswers.First(), "A")
                });

            var exam = new Exam
            {
                ExamId = examId,
                Title = "Fallback Analytics",
                Papers = new List<Paper>
                {
                    new Paper
                    {
                        PaperId = 10,
                        ExamId = examId,
                        Code = 1,
                        Questions = new List<Question> { q1 },
                        Submissions = new List<Submission> { nullAnswersSubmission, validSubmission }
                    }
                },
                ConcurrencyStamp = Array.Empty<byte>(),
                UpdatedAtUtc = DateTime.UtcNow
            };

            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId))
                              .ReturnsAsync(exam);

            var result = await _service.GetExamAnalyticsDetailAsync(examId);

            Assert.Equal(2, result.TotalSubmissions);
            Assert.Single(result.StudentResults);
            Assert.Equal("HS #1", result.StudentResults[0].StudentName);
            Assert.NotEmpty(result.HardestQuestions);

            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamAnalyticsDetailAsync - UTCID08 - Student null và QuestionAnswer null -> vẫn phân tích được phần hợp lệ")]
        public async Task GetExamAnalyticsDetailAsync_UTCID08_NullStudentAndNullQuestionAnswer_ShouldCoverRemainingBranches()
        {
            int examId = 8;

            var chapter = new Chapter
            {
                ChapterId = 10,
                SubjectId = 1,
                Name = "Chương 1"
            };

            var q1 = CreateQuestion(101, "Q1", chapter, 1);

            var submission = new Submission
            {
                SubmissionId = 1,
                StudentId = 1,
                PaperId = 10,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-30),
                UpdatedAtUtc = DateTime.UtcNow,
                TotalPoints = 6m,
                Status = 1,
                ConcurrencyStamp = Array.Empty<byte>(),
                Student = null!,
                StudentAnswers = new List<StudentAnswer>
                {
                    new StudentAnswer
                    {
                        StudentAnswerId = 1,
                        SubmissionId = 1,
                        QuestionAnswerId = 999,
                        Response = "X",
                        ConcurrencyStamp = Array.Empty<byte>(),
                        QuestionAnswer = null!
                    },
                    CreateStudentAnswer(2, q1.QuestionAnswers.First(), "A")
                }
            };

            var exam = new Exam
            {
                ExamId = examId,
                Title = "Null Student Analytics",
                Papers = new List<Paper>
                {
                    new Paper
                    {
                        PaperId = 10,
                        ExamId = examId,
                        Code = 1,
                        Questions = new List<Question> { q1 },
                        Submissions = new List<Submission> { submission }
                    }
                },
                ConcurrencyStamp = Array.Empty<byte>(),
                UpdatedAtUtc = DateTime.UtcNow
            };

            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId))
                              .ReturnsAsync(exam);

            var result = await _service.GetExamAnalyticsDetailAsync(examId);

            Assert.Single(result.StudentResults);
            Assert.Equal("HS #1", result.StudentResults[0].StudentName);
            Assert.NotEmpty(result.HardestQuestions);

            _analyticsRepoMock.VerifyAll();
        }

        private static Exam BuildExamWithValidAnalyticsData(int examId)
        {
            var chapter1 = new Chapter
            {
                ChapterId = 10,
                SubjectId = 1,
                Name = "Chương 1"
            };

            var chapter2 = new Chapter
            {
                ChapterId = 20,
                SubjectId = 1,
                Name = "Chương 2"
            };

            var q1 = CreateQuestion(101, "Q1", chapter1, 1);
            var q2 = CreateQuestion(102, "Q2", chapter2, 3);

            var student1 = new User
            {
                UserId = 1,
                Email = "s1@x.com",
                FullName = "Student 1",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var student2 = new User
            {
                UserId = 2,
                Email = "s2@x.com",
                FullName = "Student 2",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var submission1 = CreateSubmission(
                submissionId: 1,
                studentId: 1,
                updatedAtUtc: new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc),
                totalPoints: 8m,
                student: student1,
                answers: new List<StudentAnswer>
                {
                    CreateStudentAnswer(1, q1.QuestionAnswers.First(), "A"), // correct
                    CreateStudentAnswer(2, q2.QuestionAnswers.First(), "B")  // wrong
                });

            var submission2 = CreateSubmission(
                submissionId: 2,
                studentId: 2,
                updatedAtUtc: new DateTime(2026, 4, 1, 9, 30, 0, DateTimeKind.Utc),
                totalPoints: 6m,
                student: student2,
                answers: new List<StudentAnswer>
                {
                    CreateStudentAnswer(3, q1.QuestionAnswers.First(), "A"), // correct
                    CreateStudentAnswer(4, q2.QuestionAnswers.First(), "A")  // correct
                });

            return new Exam
            {
                ExamId = examId,
                Title = "Midterm Math",
                Papers = new List<Paper>
                {
                    new Paper
                    {
                        PaperId = 10,
                        ExamId = examId,
                        Code = 1,
                        Questions = new List<Question> { q1, q2 },
                        Submissions = new List<Submission> { submission1, submission2 }
                    }
                },
                ConcurrencyStamp = Array.Empty<byte>(),
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        private static Question CreateQuestion(int questionId, string content, Chapter chapter, int difficulty)
        {
            var question = new Question
            {
                QuestionId = questionId,
                CreatedByUserId = 99,
                QuestionType = "MCQ",
                QuestionContent = content,
                ChapterId = chapter.ChapterId,
                Difficulty = difficulty,
                UpdatedAtUtc = DateTime.UtcNow,
                Status = "Active",
                ConcurrencyStamp = Array.Empty<byte>(),
                Chapter = chapter,
                CreatedByUser = new User
                {
                    UserId = 99,
                    Email = "teacher@x.com",
                    FullName = "Teacher",
                    ConcurrencyStamp = Array.Empty<byte>()
                }
            };

            var qa = new QuestionAnswer
            {
                QuestionAnswerId = questionId * 10,
                QuestionId = questionId,
                Content = "Option A",
                CorrectAnswer = "A",
                IsCorrect = true,
                ConcurrencyStamp = Array.Empty<byte>(),
                Question = question
            };

            question.QuestionAnswers = new List<QuestionAnswer> { qa };
            return question;
        }

        private static Submission CreateSubmission(
            int submissionId,
            int studentId,
            DateTime updatedAtUtc,
            decimal? totalPoints,
            User student,
            List<StudentAnswer> answers)
        {
            var submission = new Submission
            {
                SubmissionId = submissionId,
                StudentId = studentId,
                PaperId = 10,
                CreatedAtUtc = updatedAtUtc.AddMinutes(-30),
                UpdatedAtUtc = updatedAtUtc,
                TotalPoints = totalPoints,
                Status = 1,
                ConcurrencyStamp = Array.Empty<byte>(),
                Student = student,
                StudentAnswers = answers
            };

            foreach (var answer in answers)
            {
                answer.Submission = submission;
            }

            return submission;
        }

        private static StudentAnswer CreateStudentAnswer(int studentAnswerId, QuestionAnswer questionAnswer, string response)
        {
            return new StudentAnswer
            {
                StudentAnswerId = studentAnswerId,
                SubmissionId = 0,
                QuestionAnswerId = questionAnswer.QuestionAnswerId,
                Response = response,
                ConcurrencyStamp = Array.Empty<byte>(),
                QuestionAnswer = questionAnswer
            };
        }
    }
}