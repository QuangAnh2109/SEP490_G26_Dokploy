namespace Backend.DTOs.Question
{
    // ═══════════════════════════════════
    //  QUESTION DTOs
    // ═══════════════════════════════════

    public class QuestionDto
    {
        public string? QuestionType { get; set; }
        public string? Stem { get; set; }
        public string? Frame { get; set; }
        public string? Explanation { get; set; }
        public int? ChapterId { get; set; }
        public int? Difficulty { get; set; }
        public byte? QuestionPurpose { get; set; }
        public string? Status { get; set; }
        public List<AnswerDto>? Answers { get; set; }
        public List<GroupAnswerDto>? BlankGroups { get; set; }
    }

    public class AnswerDto
    {
        public int? AnswerId { get; set; }
        public string? Content { get; set; }
        public string? CorrectAnswer { get; set; }
        public bool? IsCorrect { get; set; }
        public int? InputTypeId { get; set; }
        public int? BlankIndex { get; set; }
        public int? Point { get; set; }
    }

    public class GroupAnswerDto
    {
        public int? GroupAnswerId { get; set; }
        public string? Name { get; set; }
        public int? DependsOnGroupId { get; set; }
        public int? DependsOnGroupIndex { get; set; }
        public List<int>? SegmentIndices { get; set; }
        public List<int>? BlankIndices { get; set; }
    }

    public class QuestionSummaryDto
    {
        public int QuestionId { get; set; }
        public string ContentPreview { get; set; } = null!;
        public string QuestionType { get; set; } = null!;
        public int Difficulty { get; set; }
        public string DifficultyLabel { get; set; } = null!;
        public string SubjectCode { get; set; } = null!;
        public string ChapterName { get; set; } = null!;
        public DateTime UpdatedAt { get; set; }
        public string Status { get; set; } = null!;
        public byte QuestionPurpose { get; set; }
        public string QuestionPurposeLabel { get; set; } = null!;
        public int AnswerCount { get; set; }
    }

    // ═══════════════════════════════════
    //  OPERATIONAL DTOs
    // ═══════════════════════════════════

    public class QuestionStatusUpdateDto
    {
        public List<int>? QuestionIds { get; set; }
        public string? Status { get; set; }
    }

    public class QuestionListQueryDto
    {
        public string? Keyword { get; set; }
        public string? QuestionType { get; set; }
        public int? Difficulty { get; set; }
        public int? ChapterId { get; set; }
        public int? SubjectId { get; set; }
        public string? Status { get; set; }
        public byte? QuestionPurpose { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class QuestionListResultDto
    {
        public List<QuestionSummaryDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
    }

    public class QuestionMetadataDto
    {
        public List<InputTypeDto> InputTypes { get; set; } = new();
        public List<SubjectWithChaptersDto> Subjects { get; set; } = new();
    }

    public class InputTypeDto
    {
        public int InputTypeId { get; set; }
        public string Name { get; set; } = null!;
        public string? GroupType { get; set; }
    }

    public class SubjectWithChaptersDto
    {
        public int SubjectId { get; set; }
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
        public List<ChapterDto> Chapters { get; set; } = new();
    }

    public class ChapterDto
    {
        public int ChapterId { get; set; }
        public string Name { get; set; } = null!;
    }
}
