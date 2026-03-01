using Backend.DTOs.StudentExam;
using Backend.Models;

namespace Backend.Repositories.Interfaces
{
    public interface IStudentExamRepository
    {
        Task<Paper?> GetPaperWithQuestionsAsync(int examId, int paperId);
        Task<Submission> CreateSubmissionAsync(Submission submission);
        Task<Submission?> GetAnyActiveSubmissionAsync(int studentId);
        Task<StudentAnswer?> GetStudentAnswerAsync(int submissionId, int questionIndex);
        Task AddOrUpdateBulkStudentAnswersAsync(IEnumerable<StudentAnswer> answers);
        Task CompleteSubmissionAsync(int submissionId);
        Task<int> GetExamSubmissionCountAsync(int studentId, int examId);
        Task<Paper?> GetPaperWithExamAsync(int paperId);
        Task<Paper?> GetRandomPaperForExamAsync(int examId);
        
        // Security and validations
        Task<bool> CanStudentTakeExamAsync(int studentId, int examId);
        Task<Submission?> GetSubmissionByIdAsync(int submissionId);
        Task ForceSubmitOverdueExamsAsync(int examId);

        Task<ExamPreviewData?> GetExamPreviewAsync(int examId);

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
        public List<BlueprintChapterRaw> BlueprintChapters { get; set; } = new();
    }

    public class BlueprintChapterRaw
    {
        public string ChapterName { get; set; } = string.Empty;
        public int Difficulty { get; set; }
        public int TotalOfQuestions { get; set; }
    }
}
