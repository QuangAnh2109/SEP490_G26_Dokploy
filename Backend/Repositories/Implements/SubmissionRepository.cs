using Backend.Constants;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Repositories.Implements;

public class SubmissionRepository : ISubmissionRepository
{
    private readonly MtcaSep490G26Context _context;

    public SubmissionRepository(MtcaSep490G26Context context)
    {
        _context = context;
    }

    public async Task<Submission?> GetActiveSubmissionAsync(int examId, int studentId, CancellationToken ct = default)
    {
        return await _context.Submissions
            .Include(s => s.Paper)
                .ThenInclude(p => p.Exam)
            .Include(s => s.StudentAnswers)
            .Where(s => s.StudentId == studentId
                && s.Paper.ExamId == examId
                && s.Status == SubmissionStatus.InProgress)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<HashSet<int>> GetValidQuestionAnswerIdsAsync(int paperId, CancellationToken ct = default)
    {
        // Lấy tất cả QuestionAnswerId thuộc các Question trong Paper
        var ids = await _context.Papers
            .Where(p => p.PaperId == paperId)
            .SelectMany(p => p.Questions)
            .SelectMany(q => q.QuestionAnswers)
            .Select(qa => qa.QuestionAnswerId)
            .ToListAsync(ct);

        return new HashSet<int>(ids);
    }

    public async Task<Submission?> GetSubmissionForGradingAsync(int submissionId, CancellationToken ct = default)
    {
        return await _context.Submissions
            .Include(s => s.StudentAnswers)
                .ThenInclude(sa => sa.QuestionAnswer)
                    .ThenInclude(qa => qa.Question)
                        .ThenInclude(q => q.QuestionAnswers)
                            .ThenInclude(qa => qa.GroupAnswer)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.SubmissionId == submissionId, ct);
    }

    public async Task<int> GetPaperQuestionCountAsync(int paperId, CancellationToken ct = default)
    {
        // Join entity được map dưới dạng shared-type Dictionary<string, object> (name "PaperQuestion"),
        // không phải kiểu C# PaperQuestion → _context.Set<PaperQuestion>() throw "type not in model".
        // Đếm qua skip-navigation Paper.Questions để EF tự sinh JOIN qua bảng nối.
        return await _context.Papers
            .Where(p => p.PaperId == paperId)
            .Select(p => p.Questions.Count)
            .FirstOrDefaultAsync(ct);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        return _context.Database.BeginTransactionAsync(ct);
    }

    public async Task MarkGradingFailedAsync(int submissionId, string error, CancellationToken ct = default)
    {
        await _context.Submissions
            .Where(s => s.SubmissionId == submissionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.GradingStatus, GradingStatus.Failed)
                .SetProperty(x => x.GradingError, error), ct);
    }

    public void AddStudentAnswers(IEnumerable<StudentAnswer> answers)
    {
        _context.StudentAnswers.AddRange(answers);
    }

    public void RemoveStudentAnswers(IEnumerable<StudentAnswer> answers)
    {
        _context.StudentAnswers.RemoveRange(answers);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
