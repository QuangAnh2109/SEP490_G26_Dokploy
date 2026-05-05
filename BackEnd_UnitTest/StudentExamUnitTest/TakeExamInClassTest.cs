using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs.StudentExam;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backend_UnitTest.StudentExamTests;

public class TakeExamInClass_UTCID_Tests
{
    private readonly Mock<IStudentExamRepository> _repoMock;
    private readonly StudentExamService _service;

    public TakeExamInClass_UTCID_Tests()
    {
        _repoMock = new Mock<IStudentExamRepository>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<StudentExamService>>();
        var currentUserMock = new Mock<Backend.Services.Interfaces.ICurrentUserService>();
        _service = new StudentExamService(_repoMock.Object, currentUserMock.Object, loggerMock.Object, TimeProvider.System);
    }

    [Fact(DisplayName = "TakeExamInClass - UTCID01 - Có active submission cùng exam -> tiếp tục làm bài")]
    public async Task TakeExamInClass_UTCID01_HasActiveSubmissionSameExam_ShouldReturnTakeExamDto()
    {
        const int examId = 1;
        const int studentId = 1001;

        var examInfo = BuildExamInfo(examId, new List<int> { 10 }, attempts: 1, maxAttempts: 3, shuffleQuestion: false);
        var activeSubmission = BuildSubmission(500, studentId, 10, examId);
        var paper = BuildPaper(examId, 10, shuffleQuestion: false);

        _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId)).ReturnsAsync(examInfo);
        _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId)).ReturnsAsync(activeSubmission);
        _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(examId, 10)).ReturnsAsync(paper);

        var result = await _service.TakeExamInClass(examId, studentId);

        Assert.NotNull(result);
        Assert.Equal("500", result!.SubmissionId);
        Assert.Equal("1", result.ExamId);
        Assert.Equal(2, result.Questions.Count);
        _repoMock.Verify(r => r.CreateSubmissionAsync(It.IsAny<Submission>()), Times.Never);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "TakeExamInClass - UTCID02 - ExamInfo null -> return null")]
    public async Task TakeExamInClass_UTCID02_ExamInfoNull_ShouldReturnNull()
    {
        const int examId = 1;
        const int studentId = 1001;

        _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId)).ReturnsAsync((ExamInfoForStudentDto?)null);

        var result = await _service.TakeExamInClass(examId, studentId);

        Assert.Null(result);
        _repoMock.Verify(r => r.GetAnyActiveSubmissionAsync(It.IsAny<int>()), Times.Never);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "TakeExamInClass - UTCID03 - Active submission exam khác -> throw")]
    public async Task TakeExamInClass_UTCID03_ActiveSubmissionOtherExam_ShouldThrow()
    {
        const int examId = 1;
        const int studentId = 1001;

        var examInfo = BuildExamInfo(examId, new List<int> { 10 }, attempts: 1, maxAttempts: 3, shuffleQuestion: false);
        var activeSubmission = BuildSubmission(501, studentId, 99, otherExamId: 999);

        _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId)).ReturnsAsync(examInfo);
        _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId)).ReturnsAsync(activeSubmission);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.TakeExamInClass(examId, studentId));

        Assert.Equal("Bạn đang có bài thi khác chưa nộp. Vui lòng hoàn thành hoặc nộp bài đó trước khi bắt đầu bài thi mới.", ex.Message);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "TakeExamInClass - UTCID09 - Active submission nhưng Paper = null -> throw")]
    public async Task TakeExamInClass_UTCID09_ActiveSubmissionWithoutPaper_ShouldThrow()
    {
        const int examId = 1;
        const int studentId = 1001;

        var examInfo = BuildExamInfo(examId, new List<int> { 10 }, attempts: 1, maxAttempts: 3, shuffleQuestion: false);
        var activeSubmission = new Submission
        {
            SubmissionId = 502,
            StudentId = studentId,
            PaperId = 99,
            Status = SubmissionStatus.InProgress,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            UpdatedAtUtc = DateTime.UtcNow,
            ConcurrencyStamp = Array.Empty<byte>(),
            Paper = null!
        };

        _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId)).ReturnsAsync(examInfo);
        _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId)).ReturnsAsync(activeSubmission);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.TakeExamInClass(examId, studentId));

        Assert.Equal("Bạn đang có bài thi khác chưa nộp. Vui lòng hoàn thành hoặc nộp bài đó trước khi bắt đầu bài thi mới.", ex.Message);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "TakeExamInClass - UTCID04 - Hết lượt làm bài -> throw")]
    public async Task TakeExamInClass_UTCID04_ExceedMaxAttempts_ShouldThrow()
    {
        const int examId = 1;
        const int studentId = 1001;

        var examInfo = BuildExamInfo(examId, new List<int> { 10 }, attempts: 1, maxAttempts: 1, shuffleQuestion: false);

        _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId)).ReturnsAsync(examInfo);
        _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId)).ReturnsAsync((Submission?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.TakeExamInClass(examId, studentId));

        Assert.Equal("Bạn đã hết lượt làm bài cho bài thi này.", ex.Message);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "TakeExamInClass - UTCID05 - Không có paperIds -> throw")]
    public async Task TakeExamInClass_UTCID05_NoPaperIds_ShouldThrow()
    {
        const int examId = 1;
        const int studentId = 1001;

        var examInfo = BuildExamInfo(examId, new List<int>(), attempts: 0, maxAttempts: 3, shuffleQuestion: false);

        _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId)).ReturnsAsync(examInfo);
        _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId)).ReturnsAsync((Submission?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.TakeExamInClass(examId, studentId));

        Assert.Equal("Không tìm thấy đề thi nào cho bài kiểm tra này.", ex.Message);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "TakeExamInClass - UTCID10 - MaxAttempts <= 0 và PaperIds null -> throw")]
    public async Task TakeExamInClass_UTCID10_NullPaperIdsAndUnlimitedAttempts_ShouldThrow()
    {
        const int examId = 1;
        const int studentId = 1001;

        var examInfo = BuildExamInfo(examId, null!, attempts: 999, maxAttempts: 0, shuffleQuestion: false);

        _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId)).ReturnsAsync(examInfo);
        _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId)).ReturnsAsync((Submission?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.TakeExamInClass(examId, studentId));

        Assert.Equal("Không tìm thấy đề thi nào cho bài kiểm tra này.", ex.Message);
        _repoMock.Verify(r => r.CreateSubmissionAsync(It.IsAny<Submission>()), Times.Never);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "TakeExamInClass - UTCID06 - Tạo submission mới thành công")]
    public async Task TakeExamInClass_UTCID06_CreateSubmission_ShouldReturnTakeExamDto()
    {
        const int examId = 1;
        const int studentId = 1001;

        var examInfo = BuildExamInfo(examId, new List<int> { 10 }, attempts: 0, maxAttempts: 3, shuffleQuestion: false);
        var createdSubmission = BuildSubmission(600, studentId, 10, examId);
        var paper = BuildPaper(examId, 10, shuffleQuestion: false);

        _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId)).ReturnsAsync(examInfo);
        _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId)).ReturnsAsync((Submission?)null);
        _repoMock.Setup(r => r.CreateSubmissionAsync(It.Is<Submission>(s =>
            s.StudentId == studentId && s.PaperId == 10 && s.Status == SubmissionStatus.InProgress)))
            .ReturnsAsync(createdSubmission);
        _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(examId, 10)).ReturnsAsync(paper);

        var result = await _service.TakeExamInClass(examId, studentId);

        Assert.NotNull(result);
        Assert.Equal("600", result!.SubmissionId);
        Assert.Equal(2, result.Questions.Count);
        _repoMock.Verify(r => r.CreateSubmissionAsync(It.IsAny<Submission>()), Times.Once);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "TakeExamInClass - UTCID07 - Paper null -> return null")]
    public async Task TakeExamInClass_UTCID07_PaperNull_ShouldReturnNull()
    {
        const int examId = 1;
        const int studentId = 1001;

        var examInfo = BuildExamInfo(examId, new List<int> { 10 }, attempts: 0, maxAttempts: 3, shuffleQuestion: false);
        var createdSubmission = BuildSubmission(601, studentId, 10, examId);

        _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId)).ReturnsAsync(examInfo);
        _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId)).ReturnsAsync((Submission?)null);
        _repoMock.Setup(r => r.CreateSubmissionAsync(It.IsAny<Submission>())).ReturnsAsync(createdSubmission);
        _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(examId, 10)).ReturnsAsync((Paper?)null);

        var result = await _service.TakeExamInClass(examId, studentId);

        Assert.Null(result);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "TakeExamInClass - UTCID08 - ShuffleQuestion true -> vẫn trả đủ câu hỏi")]
    public async Task TakeExamInClass_UTCID08_ShuffleQuestionTrue_ShouldReturnQuestions()
    {
        const int examId = 1;
        const int studentId = 1001;

        var examInfo = BuildExamInfo(examId, new List<int> { 10 }, attempts: 0, maxAttempts: 3, shuffleQuestion: true);
        var createdSubmission = BuildSubmission(700, studentId, 10, examId);
        var paper = BuildPaper(examId, 10, shuffleQuestion: true);

        _repoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetExamInfoForStudentAsync(examId, studentId)).ReturnsAsync(examInfo);
        _repoMock.Setup(r => r.GetAnyActiveSubmissionAsync(studentId)).ReturnsAsync((Submission?)null);
        _repoMock.Setup(r => r.CreateSubmissionAsync(It.IsAny<Submission>())).ReturnsAsync(createdSubmission);
        _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(examId, 10)).ReturnsAsync(paper);

        var result = await _service.TakeExamInClass(examId, studentId);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Questions.Count);
        Assert.Contains(result.Questions, q => q.QuestionId == "101");
        Assert.Contains(result.Questions, q => q.QuestionId == "102");
        _repoMock.VerifyAll();
    }

    private static ExamInfoForStudentDto BuildExamInfo(int examId, List<int> paperIds, int attempts, int maxAttempts, bool shuffleQuestion)
        => new()
        {
            ExamId = examId,
            Title = "Quiz 1",
            Duration = 60,
            MaxAttempts = maxAttempts,
            StudentAttempts = attempts,
            CloseAt = DateTime.UtcNow.AddHours(1),
            ShuffleQuestion = shuffleQuestion,
            PaperIds = paperIds
        };

    private static Submission BuildSubmission(int submissionId, int studentId, int paperId, int otherExamId)
        => new()
        {
            SubmissionId = submissionId,
            StudentId = studentId,
            PaperId = paperId,
            Status = SubmissionStatus.InProgress,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            UpdatedAtUtc = DateTime.UtcNow,
            ConcurrencyStamp = Array.Empty<byte>(),
            Paper = new Paper
            {
                PaperId = paperId,
                ExamId = otherExamId,
                Code = 1,
                Exam = new Exam
                {
                    ExamId = otherExamId,
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

    private static Paper BuildPaper(int examId, int paperId, bool shuffleQuestion)
    {
        var inputType = new InputType
        {
            InputTypeId = 1,
            Name = "Text",
            Regex = ".*",
            GroupType = "single"
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
            Questions = new List<Question>
            {
                BuildQuestion(101, "Question 1", inputType, true),
                BuildQuestion(102, "Question 2", inputType, false)
            }
        };
    }

    private static Question BuildQuestion(int id, string content, InputType inputType, bool isCorrect)
        => new()
        {
            QuestionId = id,
            QuestionType = "MCQ",
            QuestionContent = content,
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
                new()
                {
                    QuestionAnswerId = id + 1000,
                    QuestionId = id,
                    Content = $"Answer {id}",
                    CorrectAnswer = isCorrect ? "A" : "B",
                    GroupAnswerId = null,
                    IsCorrect = isCorrect,
                    ConcurrencyStamp = Array.Empty<byte>(),
                    BlankInputs = new List<BlankInput>
                    {
                        new()
                        {
                            QuestionAnswerId = id + 1000,
                            InputTypeId = 1,
                            ConcurrencyStamp = Array.Empty<byte>(),
                            InputType = inputType
                        }
                    }
                }
            }
        };
}
