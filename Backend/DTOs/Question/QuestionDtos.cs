namespace Backend.DTOs.Question
{
    // ═══════════════════════════════════
    //  REQUEST DTOs
    // ═══════════════════════════════════

    public class CreateQuestionBatchRequest
    {
        public List<CreateQuestionItemDto> Questions { get; set; } = new();
    }

    public class CreateQuestionItemDto
    {
        public string QuestionType { get; set; } = null!;
        public string Stem { get; set; } = null!;
        public string? Frame { get; set; }
        public string? Explanation { get; set; }
        public int ChapterId { get; set; }
        public int Difficulty { get; set; }
        public string Status { get; set; } = "Draft";
        public List<AnswerItemDto> Answers { get; set; } = new();
        public List<GroupAnswerItemDto>? BlankGroups { get; set; }
    }

    public class AnswerItemDto
    {
        public string Content { get; set; } = null!;
        public string CorrectAnswer { get; set; } = null!;
        public bool? IsCorrect { get; set; }
        public int? InputTypeId { get; set; }
        public int? BlankIndex { get; set; }
        public int Point { get; set; }
    }

    public class GroupAnswerItemDto
    {
        public string Name { get; set; } = null!;
        public List<int> SegmentIndices { get; set; } = new();
        public List<int> BlankIndices { get; set; } = new();
    }

    // ═══════════════════════════════════
    //  QUERY DTOs
    // ═══════════════════════════════════

    public class QuestionListQueryDto
    {
        public string? Keyword { get; set; }
        public string? QuestionType { get; set; }
        public int? Difficulty { get; set; }
        public int? ChapterId { get; set; }
        public int? SubjectId { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // ═══════════════════════════════════
    //  RESPONSE DTOs
    // ═══════════════════════════════════

    public class QuestionListResponseDto
    {
        public List<QuestionListItemDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
    }

    public class QuestionListItemDto
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
        public int AnswerCount { get; set; }
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

    public class CreateQuestionBatchResponse
    {
        public List<QuestionListItemDto> CreatedQuestions { get; set; } = new();
        public int Count { get; set; }
    }
}
