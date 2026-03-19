using Backend.DTOs.Analytics;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implements;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly MtcaSep490G26Context _context;

    public AnalyticsRepository(MtcaSep490G26Context context)
    {
        _context = context;
    }

    public async Task<Exam?> GetExamWithFullGraphAsync(int examId)
    {
        return await _context.Exams
            .Include(e => e.Subject)
            .FirstOrDefaultAsync(e => e.ExamId == examId);
    }

    public async Task<List<StudentSubmissionSummaryDto>> GetExamSubmissionsSummaryAsync(int examId)
    {
        return await _context.Submissions
            .Where(s => s.Paper.ExamId == examId && s.Status == 2)
            .Select(s => new StudentSubmissionSummaryDto
            {
                StudentId = s.StudentId,
                StudentName = s.Student.FullName,
                TotalPoints = s.TotalPoints,
                UpdatedAtUtc = s.UpdatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<List<ChapterStatsSummaryDto>> GetChapterStatsAsync(int examId)
    {
        return await _context.StudentAnswers
            .Where(sa => sa.Submission.Paper.ExamId == examId && sa.Submission.Status == 2)
            .GroupBy(sa => sa.QuestionAnswer.Question.Chapter.Name)
            .Select(g => new ChapterStatsSummaryDto
            {
                ChapterName = g.Key ?? "N/A",
                TotalAnswers = g.Count(),
                CorrectAnswers = g.Count(sa => sa.QuestionAnswer.IsCorrect == true)
            })
            .ToListAsync();
    }

    public async Task<List<DifficultyStatsSummaryDto>> GetDifficultyStatsAsync(int examId)
    {
        return await _context.StudentAnswers
            .Where(sa => sa.Submission.Paper.ExamId == examId && sa.Submission.Status == 2)
            .GroupBy(sa => sa.QuestionAnswer.Question.Difficulty)
            .Select(g => new DifficultyStatsSummaryDto
            {
                Difficulty = g.Key,
                TotalAnswers = g.Count(),
                CorrectAnswers = g.Count(sa => sa.QuestionAnswer.IsCorrect == true)
            })
            .ToListAsync();
    }

    public async Task<List<QuestionStatsSummaryDto>> GetHardestQuestionsAsync(int examId, int topCount)
    {
        return await _context.StudentAnswers
            .Where(sa => sa.Submission.Paper.ExamId == examId && sa.Submission.Status == 2)
            .GroupBy(sa => new
            {
                sa.QuestionAnswer.QuestionId,
                sa.QuestionAnswer.Question.QuestionContent,
                ChapterName = sa.QuestionAnswer.Question.Chapter.Name,
                sa.QuestionAnswer.Question.Difficulty
            })
            .Select(g => new QuestionStatsSummaryDto
            {
                QuestionId = g.Key.QuestionId,
                QuestionContent = g.Key.QuestionContent,
                ChapterName = g.Key.ChapterName ?? "N/A",
                Difficulty = g.Key.Difficulty,
                TotalAttempts = g.Count(),
                CorrectCount = g.Count(sa => sa.QuestionAnswer.IsCorrect == true)
            })
            .OrderBy(q => (double)q.CorrectCount / q.TotalAttempts)
            .Take(topCount)
            .ToListAsync();
    }
}
