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

public record AssignExamFiltersResponseDto(
    IReadOnlyList<SubjectOptionDto> Subjects,
    IReadOnlyList<string> Semesters
);

public class CreateAssignExamRequest
{
    public int TeacherId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Duration { get; set; }
    public bool ShowScore { get; set; } = true;
    public bool ShowAnswer { get; set; }
    public int MaxAttempts { get; set; } = 1;
    public DateTime? VisibleFrom { get; set; }
    public DateTime? OpenAt { get; set; }
    public DateTime? CloseAt { get; set; }
    public bool ShuffleQuestion { get; set; }
    public bool AllowLateSubmission { get; set; }
    public bool IsPublic { get; set; }
    public int? ClassId { get; set; }

    // "blueprint" or "manual"
    public string GenerationMode { get; set; } = "blueprint";
    public int? ExamBlueprintId { get; set; }
    public int? SubjectId { get; set; }
    public List<int> QuestionIds { get; set; } = [];
    public int PaperCount { get; set; } = 1;
    public int PaperCode { get; set; } = 1;
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

public record QuestionReviewDto(
    int QuestionId,
    string QuestionType,
    string ContentLatex,
    int Difficulty,
    string ChapterName
);

public record PaperReviewDto(
    int PaperId,
    int Code,
    IReadOnlyList<QuestionReviewDto> Questions
);

public record ExamReviewDto(
    int ExamId,
    string Title,
    string SubjectCode,
    string? Description,
    int TotalQuestions,
    int Duration,
    DateTime? OpenAt,
    DateTime? CloseAt,
    string TeacherName,
    DateTime? UpdatedAtUtc,
    int Status,
    List<BlueprintRowDto> BlueprintMatrix,
    IReadOnlyList<PaperReviewDto> Papers
);

// Reuse or define BlueprintRowDto if not available in this namespace
public record BlueprintRowDto
{
    public string ChapterName { get; init; } = "";
    public int Recognize { get; init; }
    public int Understand { get; init; }
    public int Apply { get; init; }
    public int AdvancedApply { get; init; }
    public int Total { get; init; }
}

public record SwapQuestionRequestDto(
    int PaperId,
    int OldQuestionId,
    int NewQuestionId
);
