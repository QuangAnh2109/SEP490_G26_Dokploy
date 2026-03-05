using Backend.Models;

namespace Backend.Repositories.Interfaces;

public interface ISubmissionRepository
{
    /// <summary>
    /// Tìm Submission đang InProgress cho student trong một exam cụ thể (qua Paper.ExamId).
    /// Include Paper.Exam và StudentAnswers.
    /// </summary>
    Task<Submission?> GetActiveSubmissionAsync(int examId, int studentId, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách QuestionAnswerId hợp lệ thuộc về Paper của Submission.
    /// </summary>
    Task<HashSet<int>> GetValidQuestionAnswerIdsAsync(int paperId, CancellationToken ct = default);

    void AddStudentAnswers(IEnumerable<StudentAnswer> answers);

    void RemoveStudentAnswers(IEnumerable<StudentAnswer> answers);

    Task SaveChangesAsync(CancellationToken ct = default);
}
