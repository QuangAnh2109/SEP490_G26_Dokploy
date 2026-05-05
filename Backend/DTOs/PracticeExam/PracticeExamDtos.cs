namespace Backend.DTOs.PracticeExam
{
    // ═══════════════════════════════════════════════════════════
    //  REQUEST / RESPONSE
    // ═══════════════════════════════════════════════════════════

    /// <summary>Yêu cầu tạo đề luyện tập</summary>
    public class CreatePracticeExamRequest
    {
        /// <summary>Lớp học (biết luôn môn + giáo viên)</summary>
        public int? ClassId { get; set; }

        /// <summary>Danh sách chương muốn luyện — chọn 1 hoặc nhiều</summary>
        public List<int>? ChapterIds { get; set; }

        /// <summary>Số câu hỏi mong muốn (5–30)</summary>
        public int? TotalQuestions { get; set; }
    }

    /// <summary>Kết quả tạo đề luyện tập</summary>
    public class CreatePracticeExamResponse
    {
        public int PaperId { get; set; }
        public int SubmissionId { get; set; }
        public int TotalQuestions { get; set; }
        public List<PracticeQuestionDto> Questions { get; set; } = new();
        public PracticeExamProficiencySnapshot Proficiency { get; set; } = new();
    }

    /// <summary>Kết quả khi resume đề luyện tập đang làm dở</summary>
    public class ResumePracticeExamResponse
    {
        public int PaperId { get; set; }
        public int SubmissionId { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<PracticeQuestionDto> Questions { get; set; } = new();
        /// <summary>Các câu trả lời đã lưu trước đó (nếu có)</summary>
        public List<SavedAnswerDto> SavedAnswers { get; set; } = new();
    }

    /// <summary>Câu trả lời đã lưu trước đó</summary>
    public class SavedAnswerDto
    {
        public int QuestionAnswerId { get; set; }
        public string? Response { get; set; }
    }

    /// <summary>Câu hỏi trong đề luyện tập (gửi cho FE)</summary>
    public class PracticeQuestionDto
    {
        public int QuestionId { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public string QuestionContent { get; set; } = string.Empty;
        public int Difficulty { get; set; }
        public List<PracticeAnswerOptionDto> Answers { get; set; } = new();
    }

    /// <summary>Lựa chọn đáp án (không gửi IsCorrect/CorrectAnswer cho FE)</summary>
    public class PracticeAnswerOptionDto
    {
        public int QuestionAnswerId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int? GroupAnswerId { get; set; }
        public List<PracticeInputTypeDto> InputTypes { get; set; } = new();
    }

    public class PracticeInputTypeDto
    {
        public int InputTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Regex { get; set; } = string.Empty;
        public string? GroupType { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    //  SUBMIT
    // ═══════════════════════════════════════════════════════════

    /// <summary>Nộp bài luyện tập</summary>
    public class SubmitPracticeExamRequest
    {
        public int? SubmissionId { get; set; }
        public List<PracticeStudentAnswerDto>? StudentAnswers { get; set; }
    }

    public class PracticeStudentAnswerDto
    {
        public int? QuestionAnswerId { get; set; }
        public string? Response { get; set; }
    }

    public class SubmitPracticeExamResponse
    {
        public int SubmissionId { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public double AccuracyRate { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    //  RESULT (xem đáp án từng câu)
    // ═══════════════════════════════════════════════════════════

    public class PracticeExamResultDto
    {
        public int SubmissionId { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public double AccuracyRate { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
        public List<PracticeAnswerReviewDto> AnswerReview { get; set; } = new();
        public List<ChapterProficiencyDto> ChapterStats { get; set; } = new();
    }

    public class PracticeAnswerReviewDto
    {
        public int QuestionId { get; set; }
        public int QuestionOrder { get; set; }
        public string QuestionContent { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public string ChapterName { get; set; } = string.Empty;
        public int Difficulty { get; set; }
        public bool IsCorrect { get; set; }
        public List<PracticeOptionReviewDto> Options { get; set; } = new();
    }

    public class PracticeOptionReviewDto
    {
        public int QuestionAnswerId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? StudentResponse { get; set; }
        public bool IsSelected { get; set; }
        public bool? IsCorrect { get; set; }
        public string? CorrectAnswer { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    //  ITEM LEVEL HISTORY
    // ═══════════════════════════════════════════════════════════

    public class PracticeQuestionResultDto
    {
        public int QuestionId { get; set; }
        public bool IsMastered { get; set; }
        public int CorrectCount { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    //  PROFICIENCY
    // ═══════════════════════════════════════════════════════════

    public class PracticeExamProficiencySnapshot
    {
        public List<ChapterProficiencyDto> ChapterProficiencies { get; set; } = new();
    }

    public class ChapterProficiencyDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = string.Empty;
        public List<DifficultyProficiencyDto> DifficultyBreakdown { get; set; } = new();
        public double OverallAccuracyRate { get; set; }
        public int TotalAttempted { get; set; }
        public int AvailableQuestions { get; set; }
    }

    public class DifficultyProficiencyDto
    {
        public int Difficulty { get; set; }
        public string DifficultyName { get; set; } = string.Empty;
        public int TotalAttempted { get; set; }
        public int CorrectCount { get; set; }
        public int AvailableQuestions { get; set; }
        public double AccuracyRate => TotalAttempted > 0
            ? Math.Round((double)CorrectCount / TotalAttempted * 100, 1)
            : 0;
    }

    // ═══════════════════════════════════════════════════════════
    //  HISTORY
    // ═══════════════════════════════════════════════════════════

    public class PracticeHistoryDto
    {
        public int SubmissionId { get; set; }
        public int PaperId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string? SubjectCode { get; set; }
        public List<string> ChapterNames { get; set; } = new();
        public int TotalQuestions { get; set; }
        public int? CorrectCount { get; set; }
        public double? AccuracyRate { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
