using Backend.Constants;
using Backend.DTOs.PracticeExam;
using Backend.Common;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implements
{
    public class PracticeExamRepository : IPracticeExamRepository
    {
        private readonly MtcaSep490G26Context _context;
        private readonly TimeProvider _timeProvider;

        public PracticeExamRepository(MtcaSep490G26Context context, TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }

        public async Task<Class?> GetClassWithValidationAsync(int classId, int studentId)
        {
            return await _context.Classes
                .Include(c => c.Subject)
                .Where(c => c.ClassId == classId
                         && c.ClassMembers.Any(cm => cm.StudentId == studentId && cm.MemberStatus == 1))
                .FirstOrDefaultAsync();
        }

        public async Task<List<StudentProficiencyRaw>> GetStudentProficiencyAsync(int studentId, List<int> chapterIds)
        {
            // Lấy tất cả StudentAnswers từ submissions đã nộp của sinh viên,
            // chỉ cho các chương được chỉ định
            var rawData = await _context.StudentAnswers
                .Where(sa => sa.Submission.StudentId == studentId
                          && sa.Submission.Status == SubmissionStatus.Submitted)
                .Where(sa => chapterIds.Contains(sa.QuestionAnswer.Question.ChapterId))
                .Select(sa => new
                {
                    sa.Submission.SubmissionId,
                    sa.QuestionAnswer.Question.ChapterId,
                    ChapterName = sa.QuestionAnswer.Question.Chapter.Name,
                    sa.QuestionAnswer.Question.Difficulty,
                    sa.QuestionAnswer.QuestionId,
                    sa.QuestionAnswer.IsCorrect,
                    sa.QuestionAnswer.CorrectAnswer,
                    sa.Response
                })
                .AsNoTracking()
                .ToListAsync();

            // Tính correctness cho mỗi (Question per Submission)
            // Nhóm theo SubmissionId + QuestionId → check toàn bộ answers của 1 câu
            var questionResults = rawData
                .GroupBy(x => new { x.SubmissionId, x.QuestionId, x.ChapterId, x.ChapterName, x.Difficulty })
                .Select(g =>
                {
                    bool isCorrect = g.All(a =>
                    {
                        if (!string.IsNullOrEmpty(a.CorrectAnswer) && !string.IsNullOrEmpty(a.Response))
                            return a.CorrectAnswer.Trim().Equals(a.Response.Trim(), StringComparison.OrdinalIgnoreCase);
                        if (a.IsCorrect.HasValue)
                            return a.IsCorrect.Value && !string.IsNullOrEmpty(a.Response);
                        return true;
                    });
                    return new { g.Key.ChapterId, g.Key.ChapterName, g.Key.Difficulty, IsCorrect = isCorrect };
                })
                .ToList();

            // Group theo (ChapterId, Difficulty) → tổng hợp count
            return questionResults
                .GroupBy(x => new { x.ChapterId, x.ChapterName, x.Difficulty })
                .Select(g => new StudentProficiencyRaw
                {
                    ChapterId = g.Key.ChapterId,
                    ChapterName = g.Key.ChapterName,
                    Difficulty = g.Key.Difficulty,
                    TotalAttempted = g.Count(),
                    CorrectCount = g.Count(x => x.IsCorrect)
                })
                .ToList();
        }

        public async Task<List<PracticeQuestionResultDto>> GetStudentItemLevelHistoryAsync(int studentId, List<int> chapterIds)
        {
            var rawData = await _context.StudentAnswers
                .Where(sa => sa.Submission.StudentId == studentId
                          && sa.Submission.Status == SubmissionStatus.Submitted)
                .Where(sa => chapterIds.Contains(sa.QuestionAnswer.Question.ChapterId))
                .Select(sa => new
                {
                    sa.Submission.SubmissionId,
                    sa.QuestionAnswer.QuestionId,
                    sa.QuestionAnswer.IsCorrect,
                    sa.QuestionAnswer.CorrectAnswer,
                    sa.Response
                })
                .AsNoTracking()
                .ToListAsync();

            // Tính correctness cho mỗi (Question per Submission)
            var questionResults = rawData
                .GroupBy(x => new { x.SubmissionId, x.QuestionId })
                .Select(g =>
                {
                    bool isCorrect = g.All(a =>
                    {
                        if (!string.IsNullOrEmpty(a.CorrectAnswer) && !string.IsNullOrEmpty(a.Response))
                            return a.CorrectAnswer.Trim().Equals(a.Response.Trim(), StringComparison.OrdinalIgnoreCase);
                        if (a.IsCorrect.HasValue)
                            return a.IsCorrect.Value && !string.IsNullOrEmpty(a.Response);
                        return true;
                    });
                    return new { g.Key.QuestionId, IsCorrect = isCorrect };
                })
                .ToList();

            // Group by QuestionId to determine mastery (>= 3 correct attempts)
            return questionResults
                .GroupBy(x => x.QuestionId)
                .Select(g =>
                {
                    int correctCount = g.Count(x => x.IsCorrect);
                    return new PracticeQuestionResultDto
                    {
                        QuestionId = g.Key,
                        CorrectCount = correctCount,
                        IsMastered = correctCount >= 3
                    };
                })
                .ToList();
        }

        public async Task<List<int>> GetAllPracticeQuestionIdsAsync(List<int> chapterIds, int teacherId, List<int>? difficultyLevels = null)
        {
            var query = _context.Questions
                .Where(q => chapterIds.Contains(q.ChapterId)
                         && q.QuestionPurpose == QuestionPurpose.Practice
                         && q.CreatedByUserId == teacherId
                         && q.Status == QuestionStatus.Active);

            if (difficultyLevels != null && difficultyLevels.Count > 0)
                query = query.Where(q => difficultyLevels.Contains(q.Difficulty));

            return await query.Select(q => q.QuestionId).ToListAsync();
        }

        public async Task<int> CountPracticeQuestionsAsync(int chapterId, int teacherId, List<int>? difficultyLevels = null)
        {
            var query = _context.Questions
                .Where(q => q.ChapterId == chapterId
                              && q.QuestionPurpose == QuestionPurpose.Practice
                              && q.CreatedByUserId == teacherId
                              && q.Status == QuestionStatus.Active);

            if (difficultyLevels != null && difficultyLevels.Count > 0)
                query = query.Where(q => difficultyLevels.Contains(q.Difficulty));

            return await query.CountAsync();
        }

        public async Task<Paper> CreatePracticePaperAsync(List<int> questionIds)
        {
            var paper = new Paper
            {
                ExamId = null,
                Code = null
            };
            _context.Papers.Add(paper);
            await _context.SaveChangesAsync();

            // Gắn câu hỏi vào paper (many-to-many qua PaperQuestion)
            var questions = await _context.Questions
                .Where(q => questionIds.Contains(q.QuestionId))
                .ToListAsync();

            paper.Questions = questions;
            await _context.SaveChangesAsync();

            return paper;
        }

        public async Task<Submission> CreatePracticeSubmissionAsync(int studentId, int paperId)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var submission = new Submission
            {
                StudentId = studentId,
                PaperId = paperId,
                Status = SubmissionStatus.InProgress,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();
            return submission;
        }

        public async Task<Submission?> GetPracticeSubmissionFullAsync(int submissionId, int studentId)
        {
            return await _context.Submissions
                .Include(s => s.Paper)
                    .ThenInclude(p => p.Questions)
                        .ThenInclude(q => q.Chapter)
                .Include(s => s.Paper)
                    .ThenInclude(p => p.Questions)
                        .ThenInclude(q => q.QuestionAnswers)
                            .ThenInclude(qa => qa.BlankInputs)
                                .ThenInclude(bi => bi.InputType)
                .Include(s => s.StudentAnswers)
                    .ThenInclude(sa => sa.QuestionAnswer)
                .Where(s => s.SubmissionId == submissionId
                         && s.StudentId == studentId
                         && s.Paper.ExamId == null) // Chỉ lấy bài luyện tập
                .AsNoTracking()
                .AsSplitQuery() // <-- Tránh Cartesian explosion
                .FirstOrDefaultAsync();
        }

        public async Task<Paper?> GetPracticePaperWithQuestionsAsync(int paperId)
        {
            return await _context.Papers
                .Include(p => p.Questions)
                    .ThenInclude(q => q.QuestionAnswers)
                        .ThenInclude(qa => qa.BlankInputs)
                            .ThenInclude(bi => bi.InputType)
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Chapter)
                .Where(p => p.PaperId == paperId && p.ExamId == null)
                .AsNoTracking()
                .AsSplitQuery() // <-- Tránh Cartesian explosion
                .FirstOrDefaultAsync();
        }

        public async Task<List<PracticeHistoryRaw>> GetPracticeHistoryAsync(int studentId, int? classId)
        {
            var query = _context.Submissions
                .Where(s => s.StudentId == studentId && s.Paper.ExamId == null)
                .Include(s => s.Paper)
                    .ThenInclude(p => p.Questions)
                        .ThenInclude(q => q.Chapter)
                            .ThenInclude(ch => ch.Subject)
                .Include(s => s.StudentAnswers)
                    .ThenInclude(sa => sa.QuestionAnswer)
                .AsNoTracking();

            var submissions = await query
                .OrderByDescending(s => s.CreatedAtUtc)
                .Take(50) // Limit lịch sử
                .ToListAsync();

            // Nếu có classId filter, lấy subjectId của class đó
            int? filterSubjectId = null;
            if (classId.HasValue)
            {
                filterSubjectId = await _context.Classes
                    .Where(c => c.ClassId == classId.Value)
                    .Select(c => (int?)c.SubjectId)
                    .FirstOrDefaultAsync();
            }

            var result = new List<PracticeHistoryRaw>();
            foreach (var sub in submissions)
            {
                var questions = sub.Paper?.Questions?.ToList() ?? new List<Question>();
                var subject = questions.FirstOrDefault()?.Chapter?.Subject;

                // Filter theo subject nếu có
                if (filterSubjectId.HasValue && subject?.SubjectId != filterSubjectId.Value)
                    continue;

                var chapterNames = questions
                    .Select(q => q.Chapter?.Name)
                    .Where(n => n != null)
                    .Distinct()
                    .ToList()!;

                // Tính correct count nếu đã nộp
                int? correctCount = null;
                if (sub.Status == SubmissionStatus.Submitted && sub.StudentAnswers.Any())
                {
                    correctCount = 0;
                    foreach (var question in questions.DistinctBy(q => q.QuestionId))
                    {
                        bool questionCorrect = true;
                        foreach (var qa in question.QuestionAnswers)
                        {
                            var sa = sub.StudentAnswers.FirstOrDefault(a => a.QuestionAnswerId == qa.QuestionAnswerId);
                            if (sa != null)
                            {
                                if (!AnalyticsHelper.CheckIsCorrect(qa, sa))
                                    questionCorrect = false;
                            }
                            else if (qa.IsCorrect == true)
                                questionCorrect = false;
                        }
                        if (questionCorrect) correctCount++;
                    }
                }

                result.Add(new PracticeHistoryRaw
                {
                    SubmissionId = sub.SubmissionId,
                    PaperId = sub.PaperId,
                    SubjectId = subject?.SubjectId ?? 0,
                    SubjectName = subject?.Name ?? "N/A",
                    SubjectCode = subject?.Code,
                    ChapterNames = chapterNames!,
                    TotalQuestions = questions.DistinctBy(q => q.QuestionId).Count(),
                    CorrectCount = correctCount,
                    TotalPoints = sub.TotalPoints,
                    CreatedAtUtc = sub.CreatedAtUtc,
                    UpdatedAtUtc = sub.UpdatedAtUtc,
                    Status = sub.Status
                });
            }

            return result;
        }

        public async Task<List<Chapter>> GetChaptersBySubjectIdAsync(int subjectId)
        {
            return await _context.Chapters
                .Where(c => c.SubjectId == subjectId)
                .OrderBy(c => c.ChapterId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
