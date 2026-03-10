using Backend.Constants;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

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
