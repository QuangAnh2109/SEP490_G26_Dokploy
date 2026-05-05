using Backend.DTOs.StudentExam;
using Backend.Models;

namespace Backend.Repositories.Interfaces
{
    public interface IStudentExamRepository
    {
        Task<Paper?> GetPaperWithQuestionsAsync(int examId, int paperId);
        Task<Submission> CreateSubmissionAsync(Submission submission);
        Task<Submission?> GetAnyActiveSubmissionAsync(int studentId);
        Task<StudentAnswer?> GetStudentAnswerAsync(int submissionId, int questionAnswerId);
        Task AddOrUpdateBulkStudentAnswersAsync(IEnumerable<StudentAnswer> answers);
        Task CompleteSubmissionAsync(int submissionId);
        Task<int> GetExamSubmissionCountAsync(int studentId, int examId);
        Task<Paper?> GetPaperWithExamAsync(int paperId);
        Task<Paper?> GetRandomPaperForExamAsync(int examId);
        Task<int?> GetPreviousPaperIdAsync(int studentId, int examId);
        
        // Security and validations
        Task<bool> CanStudentTakeExamAsync(int studentId, int examId);
        Task<Submission?> GetSubmissionByIdAsync(int submissionId);
        Task ForceSubmitOverdueExamsAsync(int examId);

        Task<ExamPreviewData?> GetExamPreviewAsync(int examId);

        Task<ExamInfoForStudentDto?> GetExamInfoForStudentAsync(int examId, int studentId);
        Task<Submission?> GetActiveSubmissionForExamAsync(int studentId, int examId);

        /// <summary>
        /// Lấy lịch sử bài nộp tổng hợp — projection trực tiếp, không eager load.
        /// </summary>
        Task<List<SubmissionHistoryRaw>> GetSubmissionHistoryRawAsync(int studentId, int? classId);
    }

    public class ExamPreviewData
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Duration { get; set; }
        public int Status { get; set; }
        public DateTime? OpenAt { get; set; }
        public DateTime? CloseAt { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int MaxAttempts { get; set; }
        public int PaperCount { get; set; }
        public int ShowScore { get; set; }
        public int ShowAnswer { get; set; }
        public int AnswerTimingMode { get; set; }
        public List<BlueprintChapterRaw> BlueprintChapters { get; set; } = new();
    }

    public class BlueprintChapterRaw
    {
        public string ChapterName { get; set; } = string.Empty;
        public int Difficulty { get; set; }
        public int TotalOfQuestions { get; set; }
    }

    public class SubmissionHistoryRaw
    {
        public int SubmissionId { get; set; }
        public bool IsExam { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ClassName { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int Status { get; set; }
        public decimal? TotalPoints { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public int? ExamId { get; set; }
    }
}
