using Backend.DTOs.StudentExam;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Repositories.Implements
{
    public class StudentExamRepository : IStudentExamRepository
    {
        private readonly MtcaSep490G26Context _context;
        private readonly TimeProvider _timeProvider;

        public StudentExamRepository(MtcaSep490G26Context context, TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }

        // TODO: DB_UPDATE – PaperQuestions navigation đã bị xóa, giờ dùng many-to-many Paper.Questions
        public async Task<Paper?> GetPaperWithQuestionsAsync(int examId, int paperId)
        {
            return await _context.Papers
                .Include(p => p.Exam)
                .Include(p => p.Questions)
                    .ThenInclude(q => q.QuestionAnswers)
                        .ThenInclude(qa => qa.BlankInputs)
                            .ThenInclude(bi => bi.InputType)
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
            // Chỉ check submission bài thi chính thức (Paper.ExamId != null)
            // Submission luyện tập (Paper.ExamId == null) KHÔNG block bài thi chính thức
            return await _context.Submissions
                .Include(s => s.StudentAnswers)
                .Include(s => s.Paper)
                    .ThenInclude(p => p.Exam)
                .FirstOrDefaultAsync(s => s.StudentId == studentId
                                       && s.Status == 1
                                       && s.Paper.ExamId != null);
        }

        public async Task<StudentAnswer?> GetStudentAnswerAsync(int submissionId, int questionAnswerId)
        {
            return await _context.StudentAnswers
                .FirstOrDefaultAsync(sa => sa.SubmissionId == submissionId && sa.QuestionAnswerId == questionAnswerId);
        }

        // TODO: DB_UPDATE – StudentAnswer.QuestionIndex và ResponseText đã bị xóa/đổi tên
        public async Task AddOrUpdateBulkStudentAnswersAsync(IEnumerable<StudentAnswer> answers)
        {
            if (!answers.Any()) return;

            var submissionId = answers.First().SubmissionId;
            var answerIds = answers.Select(a => a.QuestionAnswerId).ToList();

            var existingAnswers = await _context.StudentAnswers
                .Where(sa => sa.SubmissionId == submissionId && answerIds.Contains(sa.QuestionAnswerId))
                .ToDictionaryAsync(sa => sa.QuestionAnswerId);

            foreach (var answer in answers)
            {
                if (existingAnswers.TryGetValue(answer.QuestionAnswerId, out var existing))
                {
                    if (string.IsNullOrEmpty(answer.Response)) 
                    {
                        // User unchecked the option, delete the record
                        _context.StudentAnswers.Remove(existing);
                    }
                    else 
                    {
                        existing.Response = answer.Response;
                        _context.StudentAnswers.Update(existing);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(answer.Response))
                    {
                        _context.StudentAnswers.Add(answer);
                    }
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

        public async Task<int?> GetPreviousPaperIdAsync(int studentId, int examId)
        {
            return await _context.Submissions
                .Where(s => s.StudentId == studentId && s.Paper.ExamId == examId)
                .OrderByDescending(s => s.CreatedAtUtc)
                .Select(s => (int?)s.PaperId)
                .FirstOrDefaultAsync();
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

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            foreach (var sub in activeSubmissions)
            {
                if (sub.Paper?.Exam == null) continue;
                var exam = sub.Paper.Exam;
                var durationSeconds = exam.Duration * 60;
                var elapsedSeconds = (now - sub.CreatedAtUtc).TotalSeconds;
                var overDuration = elapsedSeconds > durationSeconds;
                var overCloseAt = exam.CloseAt.HasValue && now > exam.CloseAt.Value;
                if (overDuration || overCloseAt)
                {
                    sub.Status = 2; // Submitted
                    sub.UpdatedAtUtc = now;
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
                MaxAttempts = exam.MaxAttempts,
                ShowScore = exam.ShowScore,
                ShowAnswer = exam.ShowAnswer,
                AnswerTimingMode = exam.AnswerTimingMode,
                PaperCount = _context.Papers.Count(p => p.ExamId == examId)
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
                // TODO: DB_UPDATE – PaperQuestions đã bị xóa, giờ dùng Paper.Questions
                var anyPaper = await _context.Papers
                    .Include(p => p.Questions)
                    .FirstOrDefaultAsync(p => p.ExamId == examId);

                result.TotalQuestions = anyPaper?.Questions?.Count ?? 0;
            }

            return result;
        }

        public async Task<ExamInfoForStudentDto?> GetExamInfoForStudentAsync(int examId, int studentId)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            return await _context.Exams
                .Where(exam =>
                    exam.ExamId == examId &&
                    _context.ClassMembers.Any(classMember =>
                        classMember.ClassId == exam.ClassId &&
                        classMember.StudentId == studentId
                    ) &&
                    exam.OpenAt <= now &&
                    exam.CloseAt > now
                )
                .Select(e => new ExamInfoForStudentDto
                {
                    ExamId = e.ExamId,
                    Title = e.Title,
                    Duration = e.Duration,
                    MaxAttempts = e.MaxAttempts,
                    StudentAttempts = _context.Submissions.Count(s => s.StudentId == studentId && s.Paper.ExamId == e.ExamId),
                    CloseAt = e.CloseAt,
                    ShuffleQuestion = e.ShuffleQuestion,
                    PaperIds = e.Papers.Select(p => p.PaperId).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<Submission?> GetActiveSubmissionForExamAsync(int studentId, int examId)
        {
            return await _context.Submissions
                .Include(s => s.Paper)
                    .ThenInclude(p => p.Exam)
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.Status == 1 && s.Paper.ExamId == examId);
        }

        public async Task<List<SubmissionHistoryRaw>> GetSubmissionHistoryRawAsync(int studentId, int? classId)
        {
            // Nếu có classId → lấy SubjectId để filter practice
            int? filterSubjectId = null;
            if (classId.HasValue)
            {
                filterSubjectId = await _context.Classes
                    .Where(c => c.ClassId == classId.Value)
                    .Select(c => (int?)c.SubjectId)
                    .FirstOrDefaultAsync();
            }

            var query = _context.Submissions
                .Where(s => s.StudentId == studentId)
                .AsNoTracking();

            // Filter: exam → classId, practice → subjectId
            if (classId.HasValue)
            {
                query = query.Where(s =>
                    (s.Paper.ExamId != null && s.Paper.Exam!.ClassId == classId.Value) ||
                    (s.Paper.ExamId == null && s.Paper.Questions.Any(q => q.Chapter.SubjectId == filterSubjectId))
                );
            }

            return await query
                .OrderByDescending(s => s.CreatedAtUtc)
                .Take(50)
                .Select(s => new SubmissionHistoryRaw
                {
                    SubmissionId = s.SubmissionId,
                    IsExam = s.Paper.ExamId != null,
                    Title = s.Paper.ExamId != null
                        ? s.Paper.Exam!.Title
                        : string.Join(", ", s.Paper.Questions
                            .Select(q => q.Chapter.Name)
                            .Distinct()),
                    ClassName = s.Paper.ExamId != null ? s.Paper.Exam!.Class!.Name : null,
                    SubjectName = s.Paper.ExamId != null
                        ? (s.Paper.Exam!.Subject != null ? s.Paper.Exam.Subject.Name : "N/A")
                        : (s.Paper.Questions.Select(q => q.Chapter.Subject!.Name).FirstOrDefault() ?? "N/A"),
                    TotalQuestions = s.Paper.Questions.Select(q => q.QuestionId).Distinct().Count(),
                    Status = s.Status,
                    TotalPoints = s.TotalPoints,
                    CreatedAtUtc = s.CreatedAtUtc,
                    UpdatedAtUtc = s.UpdatedAtUtc,
                    ExamId = s.Paper.ExamId
                })
                .ToListAsync();
        }
    }
}

