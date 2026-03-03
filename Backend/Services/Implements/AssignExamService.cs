using Backend.DTOs;
using Backend.Enums;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Implements;

public class AssignExamService : IAssignExamService
{
    private const int TeacherRoleId = 1;
    private readonly MtcaSep490G26Context _db;

    public AssignExamService(MtcaSep490G26Context db)
    {
        _db = db;
    }

    public async Task<AssignExamFiltersResponseDto> GetFiltersAsync(
        int? teacherId,
        CancellationToken cancellationToken = default)
    {
        if (!teacherId.HasValue || teacherId.Value <= 0)
        {
            throw new ArgumentException("TeacherId is required.");
        }

        var teacherExists = await _db.Users.AnyAsync(
            x => x.UserId == teacherId.Value && x.RoleId == TeacherRoleId && x.Status == 1,
            cancellationToken);
        if (!teacherExists)
        {
            throw new KeyNotFoundException($"TeacherId {teacherId.Value} not found or is not a teacher.");
        }

        var classQuery = _db.Classes
            .Where(x => x.Status == 1 && x.TeacherId == teacherId.Value)
            .AsQueryable();

        var subjectRows = await (
            from c in classQuery
            join s in _db.Subjects on c.SubjectId equals s.SubjectId
            select new
            {
                s.SubjectId,
                Code = s.Code ?? string.Empty,
                s.Name
            }
        )
            .Distinct()
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var subjects = subjectRows
            .Select(x => new SubjectOptionDto(x.SubjectId, x.Code, x.Name))
            .ToList();

        var semesters = await classQuery
            .Select(x => x.Semester)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return new AssignExamFiltersResponseDto(subjects, semesters);
    }

    public async Task<PagedResultDto<ClassListItemDto>> GetClassesAsync(
        int? teacherId,
        string? keyword,
        string? subjectCode,
        string? semester,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        keyword = keyword?.Trim();
        subjectCode = subjectCode?.Trim();
        semester = semester?.Trim();

        var query =
            from c in _db.Classes
            join s in _db.Subjects on c.SubjectId equals s.SubjectId
            where c.Status == 1
            select new { c, s };

        if (teacherId.HasValue)
        {
            query = query.Where(x => x.c.TeacherId == teacherId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.c.Name.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(subjectCode))
        {
            query = query.Where(x => x.s.Code == subjectCode);
        }

        if (!string.IsNullOrWhiteSpace(semester))
        {
            query = query.Where(x => x.c.Semester == semester);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var pageRows = await query
            .OrderBy(x => x.c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.c.ClassId,
                x.c.Name,
                x.c.Semester,
                SubjectCode = x.s.Code ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        var classIds = pageRows.Select(x => x.ClassId).ToList();
        var memberCounts = await _db.ClassMembers
            .Where(x => classIds.Contains(x.ClassId) && x.MemberStatus == 1)
            .GroupBy(x => x.ClassId)
            .Select(g => new { ClassId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClassId, x => x.Count, cancellationToken);

        var items = pageRows
            .Select(x => new ClassListItemDto(
                x.ClassId,
                x.Name,
                x.SubjectCode,
                x.Semester,
                memberCounts.GetValueOrDefault(x.ClassId, 0)))
            .ToList();

        return new PagedResultDto<ClassListItemDto>(page, pageSize, totalItems, items);
    }

    public async Task<IReadOnlyList<BlueprintListItemDto>> GetBlueprintsAsync(
        int? teacherId,
        string? subjectCode,
        string? keyword,
        CancellationToken cancellationToken = default)
    {
        subjectCode = subjectCode?.Trim();
        keyword = keyword?.Trim();

        var query =
            from b in _db.ExamBlueprints
            join s in _db.Subjects on b.SubjectId equals s.SubjectId
            where b.Status == 1
            select new { b, s };

        if (teacherId.HasValue)
        {
            query = query.Where(x => x.b.TeacherId == teacherId.Value);
        }

        if (!string.IsNullOrWhiteSpace(subjectCode))
        {
            query = query.Where(x => x.s.Code == subjectCode);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.b.Name.Contains(keyword));
        }

        return await query
            .OrderByDescending(x => x.b.UpdatedAtUtc)
            .Select(x => new BlueprintListItemDto(
                x.b.ExamBlueprintId,
                x.b.Name,
                x.s.Code ?? string.Empty,
                x.b.UpdatedAtUtc,
                x.b.TotalQuestions))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BlueprintDetailRowDto>> GetBlueprintDetailAsync(
        int blueprintId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from bc in _db.ExamBlueprintChapters
            join ch in _db.Chapters on bc.ChapterId equals ch.ChapterId
            where bc.ExamBlueprintId == blueprintId
            orderby ch.ChapterId, bc.Difficulty
            select new BlueprintDetailRowDto(
                bc.ChapterId,
                ch.Name,
                bc.Difficulty,
                bc.TotalOfQuestions)
        ).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QuestionListItemDto>> GetQuestionsAsync(
        int? teacherId,
        string? subjectCode,
        int? chapterId,
        int? difficulty,
        CancellationToken cancellationToken = default)
    {
        subjectCode = subjectCode?.Trim();

        var query =
            from q in _db.Questions
            join ch in _db.Chapters on q.ChapterId equals ch.ChapterId
            join s in _db.Subjects on ch.SubjectId equals s.SubjectId
            where q.Status == "Active"
            select new { q, ch, s };

        if (!string.IsNullOrWhiteSpace(subjectCode))
        {
            query = query.Where(x => x.s.Code == subjectCode);
        }

        if (chapterId.HasValue)
        {
            query = query.Where(x => x.q.ChapterId == chapterId.Value);
        }

        if (difficulty.HasValue)
        {
            query = query.Where(x => x.q.Difficulty == difficulty.Value);
        }

        if (teacherId.HasValue && teacherId.Value > 0)
        {
            query = query.Where(x => x.q.CreatedByUserId == teacherId.Value);
        }

        return await query
            .OrderByDescending(x => x.q.UpdatedAtUtc)
            .Select(x => new QuestionListItemDto(
                x.q.QuestionId,
                x.q.QuestionType,
                x.q.QuestionContent, // TODO: DB_UPDATE – ContentLatex đổi thành QuestionContent
                x.s.Code ?? string.Empty,
                x.ch.ChapterId,
                x.ch.Name,
                x.q.Difficulty))
            .ToListAsync(cancellationToken);
    }

    public async Task<CreateAssignExamResponse> CreateAssignExamAsync(
        CreateAssignExamRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTimeWindow(request.VisibleFrom, request.OpenAt, request.CloseAt);

        if (request.TeacherId <= 0)
        {
            throw new ArgumentException("TeacherId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Title is required.");
        }

        if (request.Duration <= 0)
        {
            throw new ArgumentException("Duration must be greater than 0.");
        }

        if (request.MaxAttempts <= 0)
        {
            throw new ArgumentException("MaxAttempts must be greater than 0.");
        }

        if (request.PaperCount <= 0)
        {
            throw new ArgumentException("PaperCount must be greater than 0.");
        }

        var teacherExists = await _db.Users.AnyAsync(
            x => x.UserId == request.TeacherId && x.RoleId == TeacherRoleId && x.Status == 1,
            cancellationToken);
        if (!teacherExists)
        {
            throw new KeyNotFoundException($"TeacherId {request.TeacherId} not found or is not a teacher.");
        }

        var mode = (request.GenerationMode ?? "blueprint").Trim().ToLowerInvariant();
        if (mode is not ("blueprint" or "manual"))
        {
            throw new ArgumentException("GenerationMode must be 'blueprint' or 'manual'.");
        }

        var (subjectId, blueprintId, questionIds) = mode == "blueprint"
            ? await BuildFromBlueprintAsync(request.ExamBlueprintId, false, cancellationToken)
            : await BuildFromManualAsync(request.SubjectId, request.QuestionIds, false, cancellationToken);

        if (!request.IsPublic && !request.ClassId.HasValue)
        {
            throw new ArgumentException("ClassId is required when creating non-public exam.");
        }

        if (request.IsPublic && request.ClassId.HasValue)
        {
            throw new ArgumentException("Public exam must not include ClassId.");
        }

        if (request.ClassId.HasValue)
        {
            var classEntity = await _db.Classes
                .FirstOrDefaultAsync(x => x.ClassId == request.ClassId.Value, cancellationToken);
            if (classEntity is null)
            {
                throw new KeyNotFoundException($"ClassId {request.ClassId.Value} not found.");
            }

            if (classEntity.TeacherId != request.TeacherId)
            {
                throw new ArgumentException("Class does not belong to this teacher.");
            }

            if (classEntity.SubjectId != subjectId)
            {
                throw new ArgumentException("Selected class subject does not match exam subject.");
            }
        }

        if (questionIds.Count == 0)
        {
            throw new ArgumentException("No questions available to create paper.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var exam = new Exam
            {
                ExamBlueprintId = blueprintId,
                TeacherId = request.TeacherId,
                ClassId = request.ClassId,
                Title = request.Title,
                SubjectId = subjectId,
                Description = request.Description,
                Duration = request.Duration,
                ShowScore = request.ShowScore,
                ShowAnswer = request.ShowAnswer,
                MaxAttempts = request.MaxAttempts,
                VisibleFrom = request.VisibleFrom,
                OpenAt = request.OpenAt,
                CloseAt = request.CloseAt,
                ShuffleQuestion = request.ShuffleQuestion,
                AllowLateSubmission = request.AllowLateSubmission,
                Status = (int)ExamStatus.Ready,
                UpdatedAtUtc = DateTime.UtcNow
            };
            _db.Exams.Add(exam);
            await _db.SaveChangesAsync(cancellationToken);

            var createdPapers = new List<CreatedPaperDto>();
            var startCode = request.PaperCode > 0 ? request.PaperCode : 1;

            for (var i = 0; i < request.PaperCount; i++)
            {
                var paper = new Paper
                {
                    ExamId = exam.ExamId,
                    Code = startCode + i
                };
                _db.Papers.Add(paper);
                await _db.SaveChangesAsync(cancellationToken);

                var orderedQuestionIds = request.ShuffleQuestion
                    ? ShuffleQuestionIds(questionIds)
                    : questionIds.ToList();

                // TODO: DB_UPDATE – PaperQuestions là implicit join table, không còn DbSet trực tiếp
                // Cần dùng Paper.Questions (many-to-many) để thêm câu hỏi vào paper
                // var paperQuestions = orderedQuestionIds
                //     .Select((qid, idx) => new PaperQuestion
                //     {
                //         PaperId = paper.PaperId,
                //         QuestionId = qid,
                //         Index = idx + 1
                //     });
                // _db.PaperQuestions.AddRange(paperQuestions);
                var questions = await _db.Questions
                    .Where(q => orderedQuestionIds.Contains(q.QuestionId))
                    .ToListAsync(cancellationToken);
                foreach (var q in questions)
                {
                    paper.Questions.Add(q);
                }
                await _db.SaveChangesAsync(cancellationToken);

                createdPapers.Add(new CreatedPaperDto(paper.PaperId, paper.Code));
            }

            await tx.CommitAsync(cancellationToken);
            return new CreateAssignExamResponse(
                exam.ExamId,
                createdPapers.First().PaperId,
                questionIds.Count,
                createdPapers);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<(int SubjectId, int? BlueprintId, List<int> QuestionIds)> BuildFromBlueprintAsync(
        int? examBlueprintId,
        bool shuffle,
        CancellationToken cancellationToken)
    {
        if (!examBlueprintId.HasValue)
        {
            throw new ArgumentException("ExamBlueprintId is required for blueprint mode.");
        }

        var blueprint = await _db.ExamBlueprints
            .FirstOrDefaultAsync(x => x.ExamBlueprintId == examBlueprintId.Value, cancellationToken);
        if (blueprint is null)
        {
            throw new KeyNotFoundException($"ExamBlueprintId {examBlueprintId.Value} not found.");
        }

        var blueprintRows = await _db.ExamBlueprintChapters
            .Where(x => x.ExamBlueprintId == examBlueprintId.Value)
            .OrderBy(x => x.ChapterId)
            .ThenBy(x => x.Difficulty)
            .ToListAsync(cancellationToken);

        var questionIds = new List<int>();
        foreach (var row in blueprintRows)
        {
            var picked = await _db.Questions
                .Where(q =>
                    q.Status == "Active" &&
                    q.ChapterId == row.ChapterId &&
                    q.Difficulty == row.Difficulty)
                .OrderBy(q => q.QuestionId)
                .Take(row.TotalOfQuestions)
                .Select(q => q.QuestionId)
                .ToListAsync(cancellationToken);

            if (picked.Count < row.TotalOfQuestions)
            {
                throw new InvalidOperationException(
                    $"Not enough active questions for chapter {row.ChapterId}, difficulty {row.Difficulty}. " +
                    $"Need {row.TotalOfQuestions}, found {picked.Count}.");
            }
            questionIds.AddRange(picked);
        }

        if (shuffle)
        {
            questionIds = questionIds.OrderBy(_ => Guid.NewGuid()).ToList();
        }

        return (blueprint.SubjectId, blueprint.ExamBlueprintId, questionIds);
    }

    private async Task<(int SubjectId, int? BlueprintId, List<int> QuestionIds)> BuildFromManualAsync(
        int? subjectId,
        IReadOnlyCollection<int> questionIds,
        bool shuffle,
        CancellationToken cancellationToken)
    {
        if (questionIds.Count == 0)
        {
            throw new ArgumentException("QuestionIds is required for manual mode.");
        }

        var selected = await (
            from q in _db.Questions
            join ch in _db.Chapters on q.ChapterId equals ch.ChapterId
            where questionIds.Contains(q.QuestionId) && q.Status == "Active"
            select new { q.QuestionId, SubjectId = ch.SubjectId }
        ).ToListAsync(cancellationToken);

        var pickedIds = selected.Select(x => x.QuestionId).Distinct().ToList();
        if (pickedIds.Count != questionIds.Distinct().Count())
        {
            throw new ArgumentException("Some questionIds are invalid or not active.");
        }

        var subjectSet = selected.Select(x => x.SubjectId).Distinct().ToList();
        if (subjectSet.Count != 1)
        {
            throw new ArgumentException("All manual questions must belong to the same subject.");
        }

        var resolvedSubjectId = subjectSet[0];
        if (subjectId.HasValue && subjectId.Value != resolvedSubjectId)
        {
            throw new ArgumentException("Provided SubjectId does not match selected questions.");
        }

        if (shuffle)
        {
            pickedIds = pickedIds.OrderBy(_ => Guid.NewGuid()).ToList();
        }

        return (resolvedSubjectId, null, pickedIds);
    }

    private static List<int> ShuffleQuestionIds(IReadOnlyList<int> source)
    {
        return source
            .OrderBy(_ => Guid.NewGuid())
            .ToList();
    }

    private static void ValidateTimeWindow(DateTime? visibleFrom, DateTime? openAt, DateTime? closeAt)
    {
        if (visibleFrom.HasValue && openAt.HasValue && visibleFrom.Value > openAt.Value)
        {
            throw new ArgumentException("VisibleFrom must be less than or equal to OpenAt.");
        }

        if (openAt.HasValue && closeAt.HasValue && openAt.Value >= closeAt.Value)
        {
            throw new ArgumentException("OpenAt must be less than CloseAt.");
        }
    }
}
