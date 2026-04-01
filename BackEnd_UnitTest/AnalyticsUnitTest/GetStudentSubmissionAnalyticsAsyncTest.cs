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
    public class GetStudentSubmissionAnalyticsAsync_UTCID_Tests
    {
        private readonly Mock<IAnalyticsRepository> _analyticsRepoMock;
        private readonly Mock<IStudentExamRepository> _studentExamRepoMock;
        private readonly AnalyticsService _service;

        public GetStudentSubmissionAnalyticsAsync_UTCID_Tests()
        {
            _analyticsRepoMock = new Mock<IAnalyticsRepository>(MockBehavior.Strict);
            _studentExamRepoMock = new Mock<IStudentExamRepository>(MockBehavior.Strict);
            _service = new AnalyticsService(_analyticsRepoMock.Object, _studentExamRepoMock.Object);
        }

        [Fact(DisplayName = "GetStudentSubmissionAnalyticsAsync - UTCID01 - Exam và submission hợp lệ -> trả DTO")]
        public async Task GetStudentSubmissionAnalyticsAsync_UTCID01_ValidSubmission_ShouldReturnDto()
        {
            int examId = 1;
            int studentId = 1;

            var exam = BuildExamForStudentAnalytics(examId, showScore: 1, showAnswer: 1);
            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId)).ReturnsAsync(exam);

            var result = await _service.GetStudentSubmissionAnalyticsAsync(examId, studentId);

            Assert.NotNull(result);
            Assert.Equal(examId, result.ExamId);
            Assert.Equal(1, result.SubmissionId); // latest for student 1 in this fixture
            Assert.Equal(1, result.ShowScore);
            Assert.Equal(1, result.ShowAnswer);
            Assert.Equal(2, result.TotalQuestions);
            Assert.NotEmpty(result.AnswerReview);
            Assert.NotNull(result.TotalPoints);
            Assert.NotNull(result.CorrectCount);
            Assert.NotNull(result.WrongCount);
            Assert.NotNull(result.ChapterStats);
            Assert.NotNull(result.DifficultyStats);
            Assert.NotNull(result.Recommendations);

            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetStudentSubmissionAnalyticsAsync - UTCID02 - Student không có submission -> KeyNotFoundException")]
        public async Task GetStudentSubmissionAnalyticsAsync_UTCID02_NoSubmission_ShouldThrowKeyNotFoundException()
        {
            int examId = 1;
            int studentId = 999;

            var exam = BuildExamForStudentAnalytics(examId, showScore: 1, showAnswer: 1);
            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId)).ReturnsAsync(exam);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetStudentSubmissionAnalyticsAsync(examId, studentId));

            Assert.Equal($"Không tìm thấy bài làm của học sinh {studentId} cho bài thi {examId}.", ex.Message);
            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetStudentSubmissionAnalyticsAsync - UTCID03 - Nhiều submission cùng học sinh -> lấy submission mới nhất")]
        public async Task GetStudentSubmissionAnalyticsAsync_UTCID03_MultipleSubmissions_ShouldUseLatest()
        {
            int examId = 3;
            int studentId = 1;

            var exam = BuildExamForStudentAnalytics(examId, showScore: 1, showAnswer: 1);
            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId)).ReturnsAsync(exam);

            var result = await _service.GetStudentSubmissionAnalyticsAsync(examId, studentId);

            Assert.Equal(1, result.SubmissionId);
            Assert.Equal(8m, result.TotalPoints);

            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetStudentSubmissionAnalyticsAsync - UTCID04 - ShowScore = 0 -> không trả thống kê điểm")]
        public async Task GetStudentSubmissionAnalyticsAsync_UTCID04_ShowScoreZero_ShouldHideScoreAnalytics()
        {
            int examId = 4;
            int studentId = 1;

            var exam = BuildExamForStudentAnalytics(examId, showScore: 0, showAnswer: 1);
            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId)).ReturnsAsync(exam);

            var result = await _service.GetStudentSubmissionAnalyticsAsync(examId, studentId);

            Assert.NotNull(result);
            Assert.Equal(0, result.ShowScore);
            Assert.Equal(1, result.ShowAnswer);
            Assert.Equal(2, result.TotalQuestions);
            Assert.NotEmpty(result.AnswerReview);

            Assert.Null(result.TotalPoints);
            Assert.Null(result.CorrectCount);
            Assert.Null(result.WrongCount);
            Assert.Null(result.ChapterStats);
            Assert.Null(result.DifficultyStats);
            Assert.Null(result.Recommendations);

            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetStudentSubmissionAnalyticsAsync - UTCID05 - Exam không tồn tại -> KeyNotFoundException")]
        public async Task GetStudentSubmissionAnalyticsAsync_UTCID05_ExamNotFound_ShouldThrowKeyNotFoundException()
        {
            int examId = 999;
            int studentId = 1;

            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId))
                .ReturnsAsync((Exam?)null);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetStudentSubmissionAnalyticsAsync(examId, studentId));

            Assert.Equal($"Không tìm thấy bài thi với ID {examId}.", ex.Message);
            _analyticsRepoMock.VerifyAll();
        }

        private static Exam BuildExamForStudentAnalytics(int examId, int showScore, int showAnswer)
        {
            var chapter1 = new Chapter { ChapterId = 10, SubjectId = 1, Name = "Chương 1" };
            var chapter2 = new Chapter { ChapterId = 20, SubjectId = 1, Name = "Chương 2" };

            var q1 = CreateQuestion(101, "Q1", chapter1, 1);
            var q2 = CreateQuestion(102, "Q2", chapter2, 2);

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

            // latest submission for student 1
            var subLatest = CreateSubmission(
                submissionId: 1,
                studentId: 1,
                paperId: 10,
                updatedAtUtc: new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc),
                totalPoints: 8m,
                student: student1,
                answers: new List<StudentAnswer>
                {
                    CreateStudentAnswer(1, q1.QuestionAnswers.First(), "A"),
                    CreateStudentAnswer(2, q2.QuestionAnswers.First(), "A")
                });

            var subOlder = CreateSubmission(
                submissionId: 2,
                studentId: 1,
                paperId: 10,
                updatedAtUtc: new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc),
                totalPoints: 5m,
                student: student1,
                answers: new List<StudentAnswer>
                {
                    CreateStudentAnswer(3, q1.QuestionAnswers.First(), "A")
                });

            var subStudent2 = CreateSubmission(
                submissionId: 3,
                studentId: 2,
                paperId: 10,
                updatedAtUtc: new DateTime(2026, 4, 1, 9, 30, 0, DateTimeKind.Utc),
                totalPoints: 7m,
                student: student2,
                answers: new List<StudentAnswer>
                {
                    CreateStudentAnswer(4, q1.QuestionAnswers.First(), "A")
                });

            return new Exam
            {
                ExamId = examId,
                Title = "Student Analytics Exam",
                ShowScore = showScore,
                ShowAnswer = showAnswer,
                Duration = 60,
                MaxAttempts = 1,
                AnswerTimingMode = 0,
                Status = 0,
                UpdatedAtUtc = DateTime.UtcNow,
                ConcurrencyStamp = Array.Empty<byte>(),
                Papers = new List<Paper>
                {
                    new Paper
                    {
                        PaperId = 10,
                        ExamId = examId,
                        Code = 1,
                        Questions = new List<Question> { q1, q2 },
                        Submissions = new List<Submission> { subLatest, subOlder, subStudent2 }
                    }
                }
            };
        }

        private static Question CreateQuestion(int questionId, string content, Chapter chapter, int difficulty)
        {
            var q = new Question
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
                Question = q
            };

            q.QuestionAnswers = new List<QuestionAnswer> { qa };
            return q;
        }

        private static Submission CreateSubmission(
            int submissionId,
            int studentId,
            int paperId,
            DateTime updatedAtUtc,
            decimal? totalPoints,
            User student,
            List<StudentAnswer> answers)
        {
            var submission = new Submission
            {
                SubmissionId = submissionId,
                StudentId = studentId,
                PaperId = paperId,
                CreatedAtUtc = updatedAtUtc.AddMinutes(-30),
                UpdatedAtUtc = updatedAtUtc,
                TotalPoints = totalPoints,
                Status = 2,
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

        private static StudentAnswer CreateStudentAnswer(int studentAnswerId, QuestionAnswer qa, string response)
        {
            return new StudentAnswer
            {
                StudentAnswerId = studentAnswerId,
                SubmissionId = 0,
                QuestionAnswerId = qa.QuestionAnswerId,
                Response = response,
                ConcurrencyStamp = Array.Empty<byte>(),
                QuestionAnswer = qa
            };
        }
    }
}