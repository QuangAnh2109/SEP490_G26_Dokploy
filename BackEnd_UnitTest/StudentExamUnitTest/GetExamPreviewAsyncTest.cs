using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backend_UnitTest.StudentExamTests;

public class GetExamPreviewAsync_UTCID_Tests
{
    private readonly Mock<IStudentExamRepository> _repoMock;
    private readonly StudentExamService _service;

    public GetExamPreviewAsync_UTCID_Tests()
    {
        _repoMock = new Mock<IStudentExamRepository>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<StudentExamService>>();
        var currentUserMock = new Mock<Backend.Services.Interfaces.ICurrentUserService>();
        _service = new StudentExamService(_repoMock.Object, currentUserMock.Object, loggerMock.Object, TimeProvider.System);
    }

    [Fact(DisplayName = "GetExamPreviewAsync - UTCID01 - Student authorized, status=1 -> public")]
    public async Task GetExamPreviewAsync_UTCID01_StatusPublic_ShouldReturnPreview()
    {
        const int userId = 1001;
        const int examId = 1;

        _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetExamPreviewAsync(examId)).ReturnsAsync(BuildExamPreviewData(examId, 1));

        var result = await _service.GetExamPreviewAsync(userId, examId, isTeacher: false);

        Assert.NotNull(result);
        Assert.Equal("public", result!.Status);
        Assert.Equal(2, result.BlueprintMatrix.Count);
        Assert.Equal(3, result.BlueprintMatrix.Single(x => x.ChapterName == "Chương 1").Total);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetExamPreviewAsync - UTCID02 - Student authorized, status=2 -> private")]
    public async Task GetExamPreviewAsync_UTCID02_StatusPrivate_ShouldReturnPreview()
    {
        const int userId = 1001;
        const int examId = 1;

        _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetExamPreviewAsync(examId)).ReturnsAsync(BuildExamPreviewData(examId, 2));

        var result = await _service.GetExamPreviewAsync(userId, examId, isTeacher: false);

        Assert.NotNull(result);
        Assert.Equal("private", result!.Status);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetExamPreviewAsync - UTCID03 - Student authorized, status=3 -> closed")]
    public async Task GetExamPreviewAsync_UTCID03_StatusClosed_ShouldReturnPreview()
    {
        const int userId = 1001;
        const int examId = 1;

        _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetExamPreviewAsync(examId)).ReturnsAsync(BuildExamPreviewData(examId, 3));

        var result = await _service.GetExamPreviewAsync(userId, examId, isTeacher: false);

        Assert.NotNull(result);
        Assert.Equal("closed", result!.Status);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetExamPreviewAsync - UTCID04 - Student authorized, status khác -> unknown")]
    public async Task GetExamPreviewAsync_UTCID04_StatusUnknown_ShouldReturnPreview()
    {
        const int userId = 1001;
        const int examId = 1;

        _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetExamPreviewAsync(examId)).ReturnsAsync(BuildExamPreviewData(examId, 99));

        var result = await _service.GetExamPreviewAsync(userId, examId, isTeacher: false);

        Assert.NotNull(result);
        Assert.Equal("unknown", result!.Status);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetExamPreviewAsync - UTCID05 - Teacher bypass permission check")]
    public async Task GetExamPreviewAsync_UTCID05_Teacher_ShouldBypassPermission()
    {
        const int userId = 2001;
        const int examId = 1;

        _repoMock.Setup(r => r.GetExamPreviewAsync(examId)).ReturnsAsync(BuildExamPreviewData(examId, 1));

        var result = await _service.GetExamPreviewAsync(userId, examId, isTeacher: true);

        Assert.NotNull(result);
        Assert.Equal("public", result!.Status);
        _repoMock.Verify(r => r.CanStudentTakeExamAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetExamPreviewAsync - UTCID06 - Student không được phép -> throw")]
    public async Task GetExamPreviewAsync_UTCID06_UnauthorizedStudent_ShouldThrow()
    {
        const int userId = 1001;
        const int examId = 1;

        _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetExamPreviewAsync(userId, examId, isTeacher: false));

        Assert.Equal("Bạn không thuộc lớp được chỉ định để xem bài thi này.", ex.Message);
        _repoMock.Verify(r => r.GetExamPreviewAsync(It.IsAny<int>()), Times.Never);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetExamPreviewAsync - UTCID07 - Repo trả null -> return null")]
    public async Task GetExamPreviewAsync_UTCID07_NullPreview_ShouldReturnNull()
    {
        const int userId = 2001;
        const int examId = 1;

        _repoMock.Setup(r => r.GetExamPreviewAsync(examId)).ReturnsAsync((ExamPreviewData?)null);

        var result = await _service.GetExamPreviewAsync(userId, examId, isTeacher: true);

        Assert.Null(result);
        _repoMock.VerifyAll();
    }

    private static ExamPreviewData BuildExamPreviewData(int examId, int status)
        => new()
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
                new() { ChapterName = "Chương 1", Difficulty = 1, TotalOfQuestions = 2 },
                new() { ChapterName = "Chương 1", Difficulty = 2, TotalOfQuestions = 1 },
                new() { ChapterName = "Chương 2", Difficulty = 3, TotalOfQuestions = 3 },
                new() { ChapterName = "Chương 2", Difficulty = 4, TotalOfQuestions = 4 }
            }
        };
}
