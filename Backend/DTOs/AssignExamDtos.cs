namespace Backend.DTOs;

public record PagedResultDto<T>(
    int Page,
    int PageSize,
    int TotalItems,
    IReadOnlyList<T> Items
);

public record ClassListItemDto(
    int ClassId,
    string ClassCode,
    string SubjectCode,
    string Semester,
    int StudentCount
);

public record BlueprintListItemDto(
    int ExamBlueprintId,
    string Name,
    string SubjectCode,
    DateTime UpdatedAtUtc,
    int TotalQuestions
);

public record BlueprintDetailRowDto(
    int ChapterId,
    string ChapterName,
    int Difficulty,
    int TotalOfQuestions
);

public record QuestionListItemDto(
    int QuestionId,
    string QuestionType,
    string QuestionContent,
    string SubjectCode,
    int ChapterId,
    string ChapterName,
    int Difficulty
);

public record SubjectOptionDto(
    int SubjectId,
    string Code,
    string Name
);

public class CreateAssignExamRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Duration { get; set; }
    /// <summary>0 = none, 1 = immediate, 2 = after_exam</summary>
    public int? ShowScore { get; set; }
    /// <summary>0 = none, 1 = student_only, 2 = with_correct</summary>
    public int? ShowAnswer { get; set; }
    /// <summary>0 = after_submit, 1 = after_exam</summary>
    public int? AnswerTimingMode { get; set; }
    public int? MaxAttempts { get; set; }
    public DateTime? VisibleFrom { get; set; }
    public DateTime? OpenAt { get; set; }
    public DateTime? CloseAt { get; set; }
    public bool? ShuffleQuestion { get; set; }
    public bool? IsPublic { get; set; }
    public int? ClassId { get; set; }

    // "blueprint" or "manual"
    public string? GenerationMode { get; set; }
    public int? ExamBlueprintId { get; set; }
    public int? SubjectId { get; set; }
    public List<int>? QuestionIds { get; set; }
    public int? PaperCount { get; set; }
    public int? PaperCode { get; set; }
}

public record CreatedPaperDto(
    int PaperId,
    int Code
);

public record CreateAssignExamResponse(
    int ExamId,
    int PaperId,
    int TotalQuestions,
    IReadOnlyList<CreatedPaperDto> Papers
);

public record QuestionReviewAnswerDto(
    int AnswerId,
    string Content,
    string CorrectAnswer,
    bool IsCorrect
);

public record QuestionReviewDto(
    int QuestionId,
    string QuestionType,
    string ContentLatex,
    int Difficulty,
    string ChapterName,
    IReadOnlyList<QuestionReviewAnswerDto> Answers
);

public record PaperReviewDto(
    int PaperId,
    int Code,
    IReadOnlyList<QuestionReviewDto> Questions
);

public record ExamReviewDto(
    int ExamId,
    int? ClassId,
    string Title,
    string SubjectCode,
    string? Description,
    int TotalQuestions,
    int Duration,
    DateTime? VisibleFrom,
    DateTime? OpenAt,
    DateTime? CloseAt,
    string TeacherName,
    DateTime? UpdatedAtUtc,
    int Status,
    List<BlueprintRowDto> BlueprintMatrix,
    IReadOnlyList<PaperReviewDto> Papers
);

public class UpdateExamInfoRequest
{
    public string? Title { get; set; }
    public DateTime? VisibleFrom { get; set; }
    public DateTime? OpenAt { get; set; }
    public DateTime? CloseAt { get; set; }
}

public record BlueprintRowDto
{
    public string ChapterName { get; init; } = "";
    public int Recognize { get; init; }
    public int Understand { get; init; }
    public int Apply { get; init; }
    public int AdvancedApply { get; init; }
    public int Total { get; init; }
}

public class SwapQuestionRequestDto
{
    public int? PaperId { get; set; }
    public int? OldQuestionId { get; set; }
    public int? NewQuestionId { get; set; }
    public bool? SwapGlobal { get; set; }
}
