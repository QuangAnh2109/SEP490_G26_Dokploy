namespace Backend.DTOs.StudentExam
{
    public class TakeExamDto
    {
        public string ExamId { get; set; } = null!;
        public string SubmissionId { get; set; } = null!;
        public int Duration { get; set; }
        public int Code { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<TakeExamQuestionDto> Questions { get; set; } = new();
        public List<TakeExamSavedAnswerDto> SavedAnswers { get; set; } = new();
    }

    public class TakeExamSavedAnswerDto
    {
        public int QuestionAnswerId { get; set; }
        public string? Response { get; set; }
    }

    public class TakeExamQuestionDto
    {
        public int QuestionId { get; set; }
        public string QuestionType { get; set; } = null!;
        public string QuestionContent { get; set; } = null!;
        public string? Hint { get; set; }
        public int Difficulty { get; set; }
        public List<TakeExamAnswerDto> Answers { get; set; } = new();
    }

    public class TakeExamAnswerDto
    {
        public int QuestionAnswerId { get; set; }
        public string Content { get; set; } = null!;
        public int? GroupAnswerId { get; set; }
        public List<TakeExamInputTypeDto> InputTypes { get; set; } = new();
    }

    public class TakeExamInputTypeDto
    {
        public int InputTypeId { get; set; }
        public string Name { get; set; } = null!;
        public string Regex { get; set; } = null!;
        public string? GroupType { get; set; }
    }
}
