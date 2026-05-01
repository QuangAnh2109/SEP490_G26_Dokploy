using Backend.Constants;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Linq;

namespace Backend.Repositories.Implements;

public class AssignExamRepository : IAssignExamRepository
{
    private readonly MtcaSep490G26Context _db;
    private readonly TimeProvider _timeProvider;

    public AssignExamRepository(MtcaSep490G26Context db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<bool> IsUserActiveAsync(int id, CancellationToken ct)
    {
        return await _db.Users.AnyAsync(x => x.UserId == id && x.Status == 1, ct);
    }

    //public async Task<AssignExamFiltersResponseDto> GetAssignExamFilterOptionsAsync(int teacherId, CancellationToken ct)
    //{
    //    var classes = await _db.Classes
    //        .Include(x => x.Subject)
    //        .Where(x => x.Status == 1 && x.TeacherId == teacherId)
    //        .ToListAsync(ct);

    //    var subjects = classes
    //        .Select(x => x.Subject)
    //        .Where(s => s != null)
    //        .Select(s => new SubjectOptionDto(s.SubjectId, s.Code ?? "", s.Name))
    //        .DistinctBy(x => x.SubjectId)
    //        .OrderBy(x => x.Code)
    //        .ThenBy(x => x.Name)
    //        .ToList();

    //    var semesters = classes
    //        .Where(x => !string.IsNullOrWhiteSpace(x.Semester))
    //        .Select(x => x.Semester!)
    //        .Distinct()
    //        .OrderBy(x => x)
    //        .ToList();

    //    return new AssignExamFiltersResponseDto(subjects, semesters);
    //}

    //public async Task<(List<ClassWithCount> Items, int Total)> GetPagedClassesForTeacherAsync(
    //    int? teacherId, string? kw, string? subj, string? sem, int page, int size, CancellationToken ct)
    //{
    //    var query = from c in _db.Classes
    //                join s in _db.Subjects on c.SubjectId equals s.SubjectId
    //                where c.Status == 1
    //                select new { c, s };

    //    if (teacherId.HasValue)
    //    {
    //        query = query.Where(x => x.c.TeacherId == teacherId.Value);
    //    }

    //    if (!string.IsNullOrWhiteSpace(kw))
    //    {
    //        query = query.Where(x => x.c.Name.Contains(kw));
    //    }

    //    if (!string.IsNullOrWhiteSpace(subj))
    //    {
    //        query = query.Where(x => x.s.Code == subj);
    //    }

    //    if (!string.IsNullOrWhiteSpace(sem))
    //    {
    //        query = query.Where(x => x.c.Semester == sem);
    //    }

    //    int total = await query.CountAsync(ct);

    //    var rows = await query
    //        .OrderBy(x => x.c.Name)
    //        .Skip((page - 1) * size)
    //        .Take(size)
    //        .Select(x => new {
    //            x.c.ClassId,
    //            x.c.Name,
    //            x.c.Semester,
    //            SubjectCode = x.s.Code ?? ""
    //        })
    //        .ToListAsync(ct);

    //    var classIds = rows.Select(r => r.ClassId).ToList();

    //    var counts = await _db.ClassMembers
    //        .Where(x => classIds.Contains(x.ClassId) && x.MemberStatus == 1)
    //        .GroupBy(x => x.ClassId)
    //        .Select(g => new { Key = g.Key, Count = g.Count() })
    //        .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

    //    var items = rows.Select(x => new ClassWithCount(
    //        x.ClassId,
    //        x.Name,
    //        x.Semester,
    //        x.SubjectCode,
    //        counts.GetValueOrDefault(x.ClassId, 0)
    //    )).ToList();

    //    return (items, total);
    //}

    public async Task<List<BlueprintListItemDto>> GetBlueprintsAsync(int? teacherId, string? subj, string? kw, CancellationToken ct)
    {
        var query = from b in _db.ExamBlueprints
                    join s in _db.Subjects on b.SubjectId equals s.SubjectId
                    where b.Status == 1
                    select new { b, s };

        if (teacherId.HasValue)
        {
            query = query.Where(x => x.b.TeacherId == teacherId.Value);
        }

        if (!string.IsNullOrWhiteSpace(subj))
        {
            query = query.Where(x => x.s.Code == subj);
        }

        if (!string.IsNullOrWhiteSpace(kw))
        {
            query = query.Where(x => x.b.Name.Contains(kw));
        }

        return await query
            .OrderByDescending(x => x.b.UpdatedAtUtc)
            .Select(x => new BlueprintListItemDto(
                x.b.ExamBlueprintId,
                x.b.Name,
                x.s.Code ?? "",
                x.b.UpdatedAtUtc,
                x.b.TotalQuestions
            ))
            .ToListAsync(ct);
    }

    public async Task<List<BlueprintDetailRowDto>> GetBlueprintDetailAsync(int id, CancellationToken ct)
    {
        return await (from bc in _db.ExamBlueprintChapters
                      join ch in _db.Chapters on bc.ChapterId equals ch.ChapterId
                      where bc.ExamBlueprintId == id
                      orderby ch.ChapterId, bc.Difficulty
                      select new BlueprintDetailRowDto(
                          bc.ChapterId,
                          ch.Name,
                          bc.Difficulty,
                          bc.TotalOfQuestions
                      ))
                     .ToListAsync(ct);
    }

    public async Task<List<QuestionListItemDto>> GetQuestionsAsync(
        int? teacherId, string? subj, int? ch, int? diff, string[] activeStatus, CancellationToken ct)
    {
        var query = BuildQuestionQuery(activeStatus);

        if (!string.IsNullOrEmpty(subj))
        {
            query = query.Where(z => z.s.Code == subj);
        }

        if (ch.HasValue)
        {
            query = query.Where(z => z.q.ChapterId == ch.Value);
        }

        if (diff.HasValue)
        {
            query = query.Where(z => z.q.Difficulty == diff.Value);
        }

        if (teacherId.HasValue && teacherId.Value > 0)
        {
            query = query.Where(z => z.q.CreatedByUserId == teacherId.Value);
        }

        return await query
            .OrderByDescending(z => z.q.UpdatedAtUtc)
            .ProjectToDto()
            .ToListAsync(ct);
    }

    public async Task<ExamBlueprint?> GetBlueprintWithChaptersAsync(int id, CancellationToken ct)
    {
        return await _db.ExamBlueprints
            .Include(x => x.ExamBlueprintChapters)
                .ThenInclude(c => c.Chapter)
            .FirstOrDefaultAsync(x => x.ExamBlueprintId == id && x.Status == 1, ct);
    }

    public async Task<List<int>> GetQuestionIdsForBlueprintRowAsync(int chapterId, int difficulty, int count, string[] activeStatus, CancellationToken ct)
    {
        return await _db.Questions
            .Where(q => activeStatus.Contains(q.Status) && q.ChapterId == chapterId && q.Difficulty == difficulty
                     && q.QuestionPurpose == QuestionPurpose.Exam)
            .OrderBy(q => Guid.NewGuid())
            .Take(count)
            .Select(q => q.QuestionId)
            .ToListAsync(ct);
    }

    public async Task<List<int>> GetAllQuestionIdsForBlueprintRowAsync(int chapterId, int difficulty, string[] activeStatus, CancellationToken ct)
    {
        return await _db.Questions
            .Where(q => activeStatus.Contains(q.Status) && q.ChapterId == chapterId && q.Difficulty == difficulty
                     && q.QuestionPurpose == QuestionPurpose.Exam)
            .Select(q => q.QuestionId)
            .ToListAsync(ct);
    }

    public async Task<List<QuestionSubjectDto>> GetQuestionsWithSubjectByIdsAsync(IEnumerable<int> ids, string[] activeStatus, CancellationToken ct)
    {
        return await (from q in _db.Questions
                      join c in _db.Chapters on q.ChapterId equals c.ChapterId
                      where ids.Contains(q.QuestionId) && activeStatus.Contains(q.Status)
                      select new QuestionSubjectDto(q.QuestionId, c.SubjectId))
                     .ToListAsync(ct);
    }

    public async Task<Class?> GetClassByIdAsync(int id, CancellationToken ct)
    {
        return await _db.Classes.FindAsync(new object[] { id }, ct);
    }

    public async Task<Exam> SaveExamAsync(Exam exam, CancellationToken ct)
    {
        _db.Exams.Add(exam);
        await _db.SaveChangesAsync(ct);
        return exam;
    }

    public async Task<Paper> SavePaperAsync(Paper paper, CancellationToken ct)
    {
        _db.Papers.Add(paper);
        await _db.SaveChangesAsync(ct);
        return paper;
    }

    public async Task AddPaperQuestionsAsync(int paperId, List<int> questionIds, CancellationToken ct)
    {
        if (questionIds == null || questionIds.Count == 0)
        {
            return;
        }

        // Use Distinct() to prevent duplicate hits within the same paper (Creation)
        var uniqueIds = questionIds.Distinct().ToList();

        // Optimized Batch Insert
        var values = string.Join(",", uniqueIds.Select(qid => $"({paperId}, {qid})"));
        var sql = $"INSERT INTO PaperQuestion (PaperId, QuestionId) VALUES {values}";

        await _db.Database.ExecuteSqlRawAsync(sql, ct);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct)
    {
        return await _db.Database.BeginTransactionAsync(ct);
    }

    public async Task<Exam?> GetExamReviewDataAsync(int id, CancellationToken ct)
    {
        return await _db.Exams
            .Include(x => x.Subject)
            .Include(x => x.Teacher)
            .Include(x => x.ExamBlueprint)
                .ThenInclude(b => b!.ExamBlueprintChapters)
                    .ThenInclude(bc => bc.Chapter)
            .Include(x => x.Papers)
                .ThenInclude(p => p.Questions)
                    .ThenInclude(q => q.Chapter)
            .Include(x => x.Papers)
                .ThenInclude(p => p.Questions)
                    .ThenInclude(q => q.QuestionAnswers)
            .FirstOrDefaultAsync(x => x.ExamId == id, ct);
    }

    public async Task<List<QuestionListItemDto>> GetAlternativeQuestionsAsync(
        int subjectId, int chapterId, int difficulty, string[] activeStatus, List<int> excludeIds, CancellationToken ct)
    {
        return await BuildQuestionQuery(activeStatus)
            .Where(z => z.s.SubjectId == subjectId &&
                        z.q.ChapterId == chapterId &&
                        z.q.Difficulty == difficulty &&
                        !excludeIds.Contains(z.q.QuestionId))
            .Take(50)
            .ProjectToDto()
            .ToListAsync(ct);
    }

    public async Task SwapPaperQuestionAsync(int paperId, int oldQuestionId, int newQuestionId, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM PaperQuestion WHERE PaperId = {0} AND QuestionId = {1}",
            paperId, oldQuestionId);

        // Check if question already exists in this paper to avoid PK violation
        bool exists = await _db.Papers
            .Where(p => p.PaperId == paperId)
            .AnyAsync(p => p.Questions.Any(q => q.QuestionId == newQuestionId), ct);

        if (!exists)
        {
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO PaperQuestion (PaperId, QuestionId) VALUES ({0}, {1})",
                paperId, newQuestionId);
        }
    }

    public async Task SwapExamQuestionGloballyAsync(int examId, int oldQuestionId, int newQuestionId, CancellationToken ct)
    {
        var paperIdsToProcess = await _db.Papers
            .Where(p => p.ExamId == examId && p.Questions.Any(q => q.QuestionId == oldQuestionId))
            .Select(p => p.PaperId)
            .ToListAsync(ct);

        if (paperIdsToProcess.Count == 0) return;

        // Delete old question from all papers that have it
        var idsStr = string.Join(",", paperIdsToProcess);
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM PaperQuestion WHERE PaperId IN (" + idsStr + ") AND QuestionId = {0}",
            oldQuestionId);

        // Find which papers ALREADY have the NEW question
        var papersWithNew = await _db.Papers
            .Where(p => p.ExamId == examId && p.Questions.Any(q => q.QuestionId == newQuestionId))
            .Select(p => p.PaperId)
            .ToListAsync(ct);

        // Only insert NewQuestion into papers that don't have it yet
        var targetIds = paperIdsToProcess.Except(papersWithNew).ToList();

        if (targetIds.Count > 0)
        {
            var insertBatch = string.Join(",", targetIds.Select(pid => $"({pid}, @p0)"));
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO PaperQuestion (PaperId, QuestionId) VALUES " + insertBatch,
                newQuestionId);
        }
    }

    public async Task<Paper?> GetPaperWithQuestionsAsync(int paperId, CancellationToken ct)
    {
        return await _db.Papers
            .Include(p => p.Questions)
            .Include(p => p.Exam)
            .FirstOrDefaultAsync(p => p.PaperId == paperId, ct);
    }

    public async Task<Question?> GetQuestionByIdAsync(int id, CancellationToken ct)
    {
        return await _db.Questions.FindAsync(new object[] { id }, ct);
    }

    public async Task UpdateExamStatusAsync(int id, int status, CancellationToken ct)
    {
        var exam = await _db.Exams.FindAsync(new object[] { id }, ct);
        if (exam != null)
        {
            exam.Status = status;
            exam.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            await _db.SaveChangesAsync(ct);
        }
    }
    
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateBlueprintToInprogressAsync(int examId, CancellationToken ct)
    {
        var exam = await _db.Exams.FindAsync(new object[] { examId }, ct);
        if (exam != null && exam.ExamBlueprintId.HasValue)
        {
            var bp = await _db.ExamBlueprints.FindAsync(new object[] { exam.ExamBlueprintId.Value }, ct);
            if (bp != null)
            {
                bp.Status = ExamBlueprintStatus.Inprogress;
                bp.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
                await _db.SaveChangesAsync(ct);
            }
        }
    }

    public async Task UpdateQuestionsToInprogressAsync(IEnumerable<int> questionIds, CancellationToken ct)
    {
        var ids = questionIds.ToList();
        if (ids.Count == 0) return;

        await _db.Questions
            .Where(q => ids.Contains(q.QuestionId) && q.Status == QuestionStatus.Active)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, QuestionStatus.Inprogress), ct);
    }

    public async Task<List<int>> GetAllQuestionIdsInExamAsync(int examId, CancellationToken ct)
    {
        return await _db.Papers
            .Where(p => p.ExamId == examId)
            .SelectMany(p => p.Questions)
            .Select(q => q.QuestionId)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<Exam?> GetExamByIdAsync(int id, CancellationToken ct)
    {
        return await _db.Exams.FindAsync(new object[] { id }, ct);
    }

    public async Task<bool> HasSubmissionsForExamAsync(int examId, CancellationToken ct)
    {
        return await _db.Submissions
            .AnyAsync(s => s.Paper.ExamId == examId, ct);
    }

    public async Task HardDeleteExamAsync(int examId, CancellationToken ct)
    {
        var paperIds = await _db.Papers
            .Where(p => p.ExamId == examId)
            .Select(p => p.PaperId)
            .ToListAsync(ct);

        if (paperIds.Count > 0)
        {
            // 1. Xóa PaperQuestion (many-to-many join table)
            var idsStr = string.Join(",", paperIds);
            await _db.Database.ExecuteSqlRawAsync(
                "DELETE FROM PaperQuestion WHERE PaperId IN (" + idsStr + ")", ct);

            // 2. Xóa Papers
            await _db.Papers
                .Where(p => p.ExamId == examId)
                .ExecuteDeleteAsync(ct);
        }

        // 3. Xóa Exam
        await _db.Exams
            .Where(e => e.ExamId == examId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task UpdateExamInfoAsync(int examId, string? title, DateTime? visibleFrom, DateTime? openAt, DateTime? closeAt, CancellationToken ct)
    {
        var exam = await _db.Exams.FindAsync(new object[] { examId }, ct)
            ?? throw new KeyNotFoundException("Exam not found.");

        if (title != null) exam.Title = title;
        exam.VisibleFrom = visibleFrom;
        exam.OpenAt = openAt;
        exam.CloseAt = closeAt;
        exam.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _db.SaveChangesAsync(ct);
    }

    private IQueryable<QuestionQueryRow> BuildQuestionQuery(string[] activeStatus)
    {
        return from q in _db.Questions
               join c in _db.Chapters on q.ChapterId equals c.ChapterId
               join s in _db.Subjects on c.SubjectId equals s.SubjectId
               where activeStatus.Contains(q.Status)
                  && q.QuestionPurpose == QuestionPurpose.Exam
               select new QuestionQueryRow { q = q, c = c, s = s };
    }
}

internal class QuestionQueryRow
{
    public Question q { get; set; } = null!;
    public Chapter c { get; set; } = null!;
    public Subject s { get; set; } = null!;
}

internal static class QuestionQueryExtensions
{
    public static IQueryable<QuestionListItemDto> ProjectToDto(this IQueryable<QuestionQueryRow> query)
    {
        return query.Select(z => new QuestionListItemDto(
            z.q.QuestionId,
            z.q.QuestionType,
            z.q.QuestionContent,
            z.s.Code ?? "",
            z.c.ChapterId,
            z.c.Name,
            z.q.Difficulty
        ));
    }
}
