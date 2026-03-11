using Backend.Models;

namespace Backend.Repositories.Interfaces;

public interface IAnalyticsRepository
{
    /// <summary>
    /// Lấy Exam kèm toàn bộ graph: Papers → Submissions (+ Student) → StudentAnswers → QuestionAnswer → Question → Chapter, và Papers → Questions → Chapter/QuestionAnswers.
    /// </summary>
    Task<Exam?> GetExamWithFullGraphAsync(int examId);
}
