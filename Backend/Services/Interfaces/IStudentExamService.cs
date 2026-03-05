using Backend.DTOs.StudentExam;
using Backend.Models;

namespace Backend.Services.Interfaces
{
    public interface IStudentExamService
    {
        Task<ExamPaperDto?> GetExamPaperAsync(int studentId, int examId);
        Task<Submission> StartExamAsync(int studentId, StartSubmissionRequest request);
        Task SaveAnswerAsync(int studentId, int submissionId, SubmitAnswerRequest request);
        Task SaveBulkAnswersAsync(int studentId, int submissionId, IEnumerable<SubmitAnswerRequest> requests);
        Task SubmitExamAsync(int studentId, int submissionId);
        Task ForceSubmitOverdueSubmissionsAsync(int examId);

        Task<ExamPreviewDto?> GetExamPreviewAsync(int studentId, int examId);

        Task<TakeExamDto?> TakeExamInClass(int examId, int studentId);

    }
}
