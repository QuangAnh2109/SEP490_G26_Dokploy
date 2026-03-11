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
        // 1. Tải Exam với thông tin cơ bản
        var exam = await _context.Exams
            .Include(e => e.Subject)
            .FirstOrDefaultAsync(e => e.ExamId == examId);
        if (exam == null) return null;

        // 2. Tải trực tiếp tất cả các Paper
        var papers = await _context.Papers
            .Include(p => p.Questions)
                .ThenInclude(q => q.Chapter)
            .Include(p => p.Questions)
                .ThenInclude(q => q.QuestionAnswers)
            .Where(p => p.ExamId == examId)
            .ToListAsync();

        var paperIds = papers.Select(p => p.PaperId).ToList();

        // 3. Tải tất cả Submissions liên quan
        var submissions = await _context.Submissions
            .Include(s => s.Student)
            .Where(s => paperIds.Contains(s.PaperId))
            .ToListAsync();

        var subIds = submissions.Select(s => s.SubmissionId).ToList();

        // 4. Tải StudentAnswers — TRUY VẤN PHẲNG (Flat Query)
        // Đây là bước quan trọng nhất để chống lỗi "totalAnswersFound = 0"
        var allAnswers = await _context.StudentAnswers
            .Include(sa => sa.QuestionAnswer)
                .ThenInclude(qa => qa.Question)
                    .ThenInclude(q => q.Chapter)
            .Where(sa => subIds.Contains(sa.SubmissionId))
            .ToListAsync();

        // 5. Khâu nối thủ công (Deep Stitching)
        foreach (var sub in submissions)
        {
            sub.StudentAnswers = allAnswers.Where(a => a.SubmissionId == sub.SubmissionId).ToList();
        }

        foreach (var paper in papers)
        {
            paper.Submissions = submissions.Where(s => s.PaperId == paper.PaperId).ToList();
        }
        
        exam.Papers = papers;

        return exam;
    }
}
