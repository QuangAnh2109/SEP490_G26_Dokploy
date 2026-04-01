using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backend_UnitTest.StudentExamTests
{
    public class GetExamPreviewAsync_UTCID_Tests
    {
        private readonly Mock<IStudentExamRepository> _repoMock;
        private readonly Mock<ILogger<StudentExamService>> _loggerMock;
        private readonly StudentExamService _service;

        public GetExamPreviewAsync_UTCID_Tests()
        {
            _repoMock = new Mock<IStudentExamRepository>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<StudentExamService>>();
            _service = new StudentExamService(_repoMock.Object, _loggerMock.Object);
        }

        [Fact(DisplayName = "GetExamPreviewAsync - UTCID01 - Student authorized, status=1 -> return public preview")]
        public async Task GetExamPreviewAsync_UTCID01_StudentAuthorized_StatusPublic_ShouldReturnPreview()
        {
            // Arrange
            int userId = 1001;
            int examId = 1;

            _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId))
                .ReturnsAsync(true);
            _repoMock.Setup(r => r.GetExamPreviewAsync(examId))
                .ReturnsAsync(BuildExamPreviewData(examId, status: 1));

            // Act
            var result = await _service.GetExamPreviewAsync(userId, examId, isTeacher: false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(examId, result!.ExamId);
            Assert.Equal("public", result.Status);
            Assert.Equal("MAE", result.SubjectCode);
            Assert.Equal("Exam Preview", result.Title);
            Assert.Equal(2, result.BlueprintMatrix.Count);

            var row1 = result.BlueprintMatrix.Single(x => x.ChapterName == "Chương 1");
            Assert.Equal(2, row1.Recognize);
            Assert.Equal(1, row1.Understand);
            Assert.Equal(3, row1.Total);

            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamPreviewAsync - UTCID02 - Student authorized, status=2 -> return private preview")]
        public async Task GetExamPreviewAsync_UTCID02_StudentAuthorized_StatusPrivate_ShouldReturnPreview()
        {
            int userId = 1001;
            int examId = 1;

            _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId))
                .ReturnsAsync(true);
            _repoMock.Setup(r => r.GetExamPreviewAsync(examId))
                .ReturnsAsync(BuildExamPreviewData(examId, status: 2));

            var result = await _service.GetExamPreviewAsync(userId, examId, isTeacher: false);

            Assert.NotNull(result);
            Assert.Equal("private", result!.Status);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamPreviewAsync - UTCID03 - Student authorized, status=3 -> return closed preview")]
        public async Task GetExamPreviewAsync_UTCID03_StudentAuthorized_StatusClosed_ShouldReturnPreview()
        {
            int userId = 1001;
            int examId = 1;

            _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId))
                .ReturnsAsync(true);
            _repoMock.Setup(r => r.GetExamPreviewAsync(examId))
                .ReturnsAsync(BuildExamPreviewData(examId, status: 3));

            var result = await _service.GetExamPreviewAsync(userId, examId, isTeacher: false);

            Assert.NotNull(result);
            Assert.Equal("closed", result!.Status);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamPreviewAsync - UTCID04 - Student authorized, unknown status -> return unknown preview")]
        public async Task GetExamPreviewAsync_UTCID04_StudentAuthorized_StatusUnknown_ShouldReturnPreview()
        {
            int userId = 1001;
            int examId = 1;

            _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId))
                .ReturnsAsync(true);
            _repoMock.Setup(r => r.GetExamPreviewAsync(examId))
                .ReturnsAsync(BuildExamPreviewData(examId, status: 99));

            var result = await _service.GetExamPreviewAsync(userId, examId, isTeacher: false);

            Assert.NotNull(result);
            Assert.Equal("unknown", result!.Status);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamPreviewAsync - UTCID05 - Teacher bypass permission check -> return preview")]
        public async Task GetExamPreviewAsync_UTCID05_TeacherBypassPermission_ShouldReturnPreview()
        {
            int userId = 2001;
            int examId = 1;

            _repoMock.Setup(r => r.GetExamPreviewAsync(examId))
                .ReturnsAsync(BuildExamPreviewData(examId, status: 1));

            var result = await _service.GetExamPreviewAsync(userId, examId, isTeacher: true);

            Assert.NotNull(result);
            Assert.Equal("public", result!.Status);
            _repoMock.Verify(r => r.CanStudentTakeExamAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamPreviewAsync - UTCID06 - Student not authorized -> UnauthorizedAccessException")]
        public async Task GetExamPreviewAsync_UTCID06_StudentNotAuthorized_ShouldThrowUnauthorizedAccessException()
        {
            int userId = 1001;
            int examId = 1;

            _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId))
                .ReturnsAsync(false);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetExamPreviewAsync(userId, examId, isTeacher: false));

            Assert.Equal("Bạn không thuộc lớp được chỉ định để xem bài thi này.", ex.Message);
            _repoMock.Verify(r => r.GetExamPreviewAsync(It.IsAny<int>()), Times.Never);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamPreviewAsync - UTCID07 - Repo returns null -> return null")]
        public async Task GetExamPreviewAsync_UTCID07_RepoReturnsNull_ShouldReturnNull()
        {
            int userId = 2001;
            int examId = 1;

            _repoMock.Setup(r => r.GetExamPreviewAsync(examId))
                .ReturnsAsync((ExamPreviewData?)null);

            var result = await _service.GetExamPreviewAsync(userId, examId, isTeacher: true);

            Assert.Null(result);
            _repoMock.VerifyAll();
        }

        private static ExamPreviewData BuildExamPreviewData(int examId, int status)
        {
            return new ExamPreviewData
            {
                ExamId = examId,
                Title = "Exam Preview",
                Description = "Description",
                Duration = 60,
                Status = status,
                OpenAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
                CloseAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAtUtc = new DateTime(2026, 4, 1, 7, 0, 0, DateTimeKind.Utc),
                SubjectCode = "MAE",
                SubjectName = "Math",
                TeacherName = "Teacher A",
                TotalQuestions = 10,
                MaxAttempts = 3,
                PaperCount = 2,
                ShowScore = 1,
                ShowAnswer = 1,
                AnswerTimingMode = 0,
                BlueprintChapters = new List<BlueprintChapterRaw>
                {
                    new BlueprintChapterRaw { ChapterName = "Chương 1", Difficulty = 1, TotalOfQuestions = 2 },
                    new BlueprintChapterRaw { ChapterName = "Chương 1", Difficulty = 2, TotalOfQuestions = 1 },
                    new BlueprintChapterRaw { ChapterName = "Chương 2", Difficulty = 3, TotalOfQuestions = 3 },
                    new BlueprintChapterRaw { ChapterName = "Chương 2", Difficulty = 4, TotalOfQuestions = 4 }
                }
            };
        }
    }
}