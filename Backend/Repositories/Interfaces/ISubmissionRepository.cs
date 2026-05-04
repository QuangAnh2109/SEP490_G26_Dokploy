using Backend.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Repositories.Interfaces;

public interface ISubmissionRepository
{
    /// <summary>
    /// Mở transaction trên DbContext để bao quanh chuỗi SaveChanges của grading.
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);


    /// <summary>
    /// Tìm Submission đang InProgress cho student trong một exam cụ thể (qua Paper.ExamId).
    /// Include Paper.Exam và StudentAnswers.
    /// </summary>
    Task<Submission?> GetActiveSubmissionAsync(int examId, int studentId, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách QuestionAnswerId hợp lệ thuộc về Paper của Submission.
    /// </summary>
    Task<HashSet<int>> GetValidQuestionAnswerIdsAsync(int paperId, CancellationToken ct = default);

    /// <summary>
    /// Lấy Submission kèm toàn bộ thông tin cần thiết để chấm điểm (eager-load chống N+1).
    /// </summary>
    Task<Submission?> GetSubmissionForGradingAsync(int submissionId, CancellationToken ct = default);

    /// <summary>Số câu hỏi distinct trong Paper, dùng để chia đều điểm bài thi.</summary>
    Task<int> GetPaperQuestionCountAsync(int paperId, CancellationToken ct = default);

    /// <summary>
    /// Set GradingStatus=Failed + GradingError bằng raw UPDATE, bypass change tracker
    /// để tránh save state dở dang khi grading throw giữa chừng.
    /// </summary>
    Task MarkGradingFailedAsync(int submissionId, string error, CancellationToken ct = default);

    void AddStudentAnswers(IEnumerable<StudentAnswer> answers);

    void RemoveStudentAnswers(IEnumerable<StudentAnswer> answers);

    Task SaveChangesAsync(CancellationToken ct = default);
}
