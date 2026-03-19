using Backend.Models;
using Backend.DTOs.Analytics;

namespace Backend.Repositories.Interfaces;

public interface IAnalyticsRepository
{
    /// <summary>
    /// Lấy Exam kèm toàn bộ graph: Papers → Submissions (+ Student) → StudentAnswers → QuestionAnswer → Question → Chapter, và Papers → Questions → Chapter/QuestionAnswers.
    /// </summary>
    Task<Exam?> GetExamWithFullGraphAsync(int examId);

    Task<List<StudentSubmissionSummaryDto>> GetExamSubmissionsSummaryAsync(int examId);

    Task<List<ChapterStatsSummaryDto>> GetChapterStatsAsync(int examId);

    Task<List<DifficultyStatsSummaryDto>> GetDifficultyStatsAsync(int examId);

    Task<List<QuestionStatsSummaryDto>> GetHardestQuestionsAsync(int examId, int topCount);
}
