using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implements
{
    public class StudentExamRepository : IStudentExamRepository
    {
        private readonly MtcaSep490G26Context _context;

        public StudentExamRepository(MtcaSep490G26Context context)
        {
            _context = context;
        }

        public async Task<Paper?> GetPaperWithQuestionsAsync(int examId, int paperId)
        {
            return await _context.Papers
                .Include(p => p.Exam)
                .Include(p => p.PaperQuestions)
                .ThenInclude(pq => pq.Question)
                .FirstOrDefaultAsync(p => p.ExamId == examId && p.PaperId == paperId);
        }

        public async Task<Submission> CreateSubmissionAsync(Submission submission)
        {
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();
            return submission;
        }

        public async Task<Submission?> GetAnyActiveSubmissionAsync(int studentId)
        {
            // Status 1 = Active / In Progress
            return await _context.Submissions
                .Include(s => s.StudentAnswers)
                .Include(s => s.Paper)
                    .ThenInclude(p => p.Exam)
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.Status == 1);
        }

        public async Task<StudentAnswer?> GetStudentAnswerAsync(int submissionId, int questionIndex)
        {
            return await _context.StudentAnswers
                .FirstOrDefaultAsync(sa => sa.SubmissionId == submissionId && sa.QuestionIndex == questionIndex);
        }

        public async Task AddOrUpdateBulkStudentAnswersAsync(IEnumerable<StudentAnswer> answers)
        {
            if (!answers.Any()) return;

            var submissionId = answers.First().SubmissionId;
            var indices = answers.Select(a => a.QuestionIndex).ToList();

            var existingAnswers = await _context.StudentAnswers
                .Where(sa => sa.SubmissionId == submissionId && indices.Contains(sa.QuestionIndex))
                .ToDictionaryAsync(sa => sa.QuestionIndex);

            foreach (var answer in answers)
            {
                if (existingAnswers.TryGetValue(answer.QuestionIndex, out var existing))
                {
                    existing.ResponseText = answer.ResponseText;
                    _context.StudentAnswers.Update(existing);
                }
                else
                {
                    _context.StudentAnswers.Add(answer);
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task CompleteSubmissionAsync(int submissionId)
        {
            var submission = await _context.Submissions.FindAsync(submissionId);
            if (submission != null)
            {
                submission.Status = 2; // e.g. 2 = Submitted
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetExamSubmissionCountAsync(int studentId, int examId)
        {
            return await _context.Submissions
                .Include(s => s.Paper)
                .CountAsync(s => s.StudentId == studentId && s.Paper.ExamId == examId);
        }

        public async Task<Paper?> GetPaperWithExamAsync(int paperId)
        {
            return await _context.Papers
                .Include(p => p.Exam)
                .FirstOrDefaultAsync(p => p.PaperId == paperId);
        }

        public async Task<Paper?> GetRandomPaperForExamAsync(int examId)
        {
            var paperIds = await _context.Papers
                .Where(p => p.ExamId == examId)
                .Select(p => p.PaperId)
                .ToListAsync();

            if (!paperIds.Any()) return null;

            var random = new Random();
            int randomIndex = random.Next(paperIds.Count);
            int selectedPaperId = paperIds[randomIndex];

            return await _context.Papers.FindAsync(selectedPaperId);
        }

        public async Task<bool> CanStudentTakeExamAsync(int studentId, int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null || exam.ClassId == null) return false;

            return await _context.ClassMembers
                .AnyAsync(cm => cm.ClassId == exam.ClassId && cm.StudentId == studentId);
        }

        public async Task<Submission?> GetSubmissionByIdAsync(int submissionId)
        {
            return await _context.Submissions
                .Include(s => s.Paper)
                .ThenInclude(p => p.Exam)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);
        }

        public async Task ForceSubmitOverdueExamsAsync(int examId)
        {
            var activeSubmissions = await _context.Submissions
                .Include(s => s.Paper)
                .ThenInclude(p => p.Exam)
                .Where(s => s.Paper.ExamId == examId && s.Status == 1)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var sub in activeSubmissions)
            {
                if (sub.Paper?.Exam != null)
                {
                    var durationSeconds = sub.Paper.Exam.Duration * 60;
                    var elapsedSeconds = (now - sub.CreatedAtUtc).TotalSeconds;
                    if (elapsedSeconds > durationSeconds)
                    {
                        sub.Status = 2; // Submitted
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task<ExamPreviewData?> GetExamPreviewAsync(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.Subject)
                .Include(e => e.Teacher)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null) return null;

            var result = new ExamPreviewData
            {
                ExamId = exam.ExamId,
                Title = exam.Title,
                Description = exam.Description,
                Duration = exam.Duration,
                Status = exam.Status,
                OpenAt = exam.OpenAt,
                CloseAt = exam.CloseAt,
                UpdatedAtUtc = exam.UpdatedAtUtc,
                SubjectCode = exam.Subject?.Code ?? string.Empty,
                SubjectName = exam.Subject?.Name ?? string.Empty,
                TeacherName = exam.Teacher?.FullName ?? string.Empty,
            };

            // Dùng raw SQL để lấy ExamBlueprintId vì chưa được map trong EF model
            var examBlueprintId = await _context.Database
                .SqlQuery<int?>($"SELECT ExamBlueprintId AS [Value] FROM Exams WHERE ExamId = {examId}")
                .FirstOrDefaultAsync();

            if (examBlueprintId.HasValue)
            {
                var blueprint = await _context.ExamBlueprints
                    .FirstOrDefaultAsync(bp => bp.ExamBlueprintId == examBlueprintId.Value);

                result.TotalQuestions = blueprint?.TotalQuestions ?? 0;

                var blueprintChapters = await _context.ExamBlueprintChapters
                    .Include(ebc => ebc.Chapter)
                    .Where(ebc => ebc.ExamBlueprintId == examBlueprintId.Value)
                    .ToListAsync();

                result.BlueprintChapters = blueprintChapters.Select(ebc => new BlueprintChapterRaw
                {
                    ChapterName = ebc.Chapter?.Name ?? string.Empty,
                    Difficulty = ebc.Difficulty,
                    TotalOfQuestions = ebc.TotalOfQuestions
                }).ToList();
            }
            else
            {
                var anyPaper = await _context.Papers
                    .Include(p => p.PaperQuestions)
                    .FirstOrDefaultAsync(p => p.ExamId == examId);

                result.TotalQuestions = anyPaper?.PaperQuestions?.Count ?? 0;
            }

            return result;
        }

    }
}
