using Backend.DTOs;
using Backend.Constants;
using Backend.Models;
using Backend.Services.Interfaces;
using Backend.Repositories.Interfaces;
using System.Linq;

namespace Backend.Services.Implements;

public class AssignExamService : IAssignExamService
{
    private static readonly string[] ActiveStatus = [QuestionStatus.Active, QuestionStatus.Inprogess];

    private readonly IAssignExamRepository _repo;

    public AssignExamService(IAssignExamRepository repo)
    {
        _repo = repo;
    }

    private async Task EnsureUserActiveAsync(int? id, CancellationToken ct)
    {
        if (id is null or <= 0)
        {
            throw new ArgumentException("TeacherId is required.");
        }

        bool isActive = await _repo.IsUserActiveAsync(id.Value, ct);
        if (!isActive)
        {
            throw new KeyNotFoundException($"User with Id {id} not found or is inactive.");
        }
    }

    private static void ThrowIf(bool condition, string msg)
    {
        if (condition)
        {
            throw new ArgumentException(msg);
        }
    }

    private static string Clean(string? s)
    {
        return s?.Trim() ?? string.Empty;
    }

    public async Task<AssignExamFiltersResponseDto> GetFiltersAsync(int? teacherId, CancellationToken ct = default)
    {
        await EnsureUserActiveAsync(teacherId, ct);

        return await _repo.GetAssignExamFilterOptionsAsync(teacherId!.Value, ct);
    }

    public async Task<PagedResultDto<ClassListItemDto>> GetClassesAsync(
        int? teacherId, string? kw, string? subj, string? sem, int page, int size, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 200);

        kw = kw?.Trim();
        subj = subj?.Trim();
        sem = sem?.Trim();

        var (items, total) = await _repo.GetPagedClassesForTeacherAsync(teacherId, kw, subj, sem, page, size, ct);

        var classListItems = items.Select(x => new ClassListItemDto(
            x.ClassId,
            x.Name,
            x.SubjectCode,
            x.Semester,
            x.MemberCount
        )).ToList();

        return new PagedResultDto<ClassListItemDto>(page, size, total, classListItems);
    }

    public async Task<IReadOnlyList<BlueprintListItemDto>> GetBlueprintsAsync(
        int? teacherId, string? subj, string? kw, CancellationToken ct = default)
    {
        subj = subj?.Trim();
        kw = kw?.Trim();

        return await _repo.GetBlueprintsAsync(teacherId, subj, kw, ct);
    }

    public async Task<IReadOnlyList<BlueprintDetailRowDto>> GetBlueprintDetailAsync(int id, CancellationToken ct = default)
    {
        return await _repo.GetBlueprintDetailAsync(id, ct);
    }

    public async Task<IReadOnlyList<QuestionListItemDto>> GetQuestionsAsync(
        int? teacherId, string? subj, int? ch, int? diff, CancellationToken ct = default)
    {
        subj = subj?.Trim();
        return await _repo.GetQuestionsAsync(teacherId, subj, ch, diff, ActiveStatus, ct);
    }

    public async Task<CreateAssignExamResponse> CreateAssignExamAsync(CreateAssignExamRequest r, CancellationToken ct = default)
    {
        ValidateTimeWindow(r.VisibleFrom, r.OpenAt, r.CloseAt);
        await EnsureUserActiveAsync(r.TeacherId, ct);

        ThrowIf(string.IsNullOrWhiteSpace(r.Title), "Title is required.");
        ThrowIf(r.Duration <= 0, "Duration must be > 0.");
        ThrowIf(r.MaxAttempts <= 0, "MaxAttempts must be > 0.");
        ThrowIf(r.PaperCount <= 0, "PaperCount must be > 0.");

        string generationMode = Clean(r.GenerationMode).ToLower();
        var mode = generationMode == "manual" ? "manual" : "blueprint";

        int subjectId;
        int? blueprintId;
        List<int> questionIds;

        if (mode == "blueprint")
        {
            var result = await BuildFromBlueprintAsync(r.ExamBlueprintId, ct);
            subjectId = result.SubjId;
            blueprintId = result.BpId;
            questionIds = result.QIds;
        }
        else
        {
            var result = await BuildFromManualAsync(r.SubjectId, r.QuestionIds, ct);
            subjectId = result.SubjId;
            blueprintId = result.BpId;
            questionIds = result.QIds;
        }

        ThrowIf(!r.IsPublic && !r.ClassId.HasValue, "ClassId required for non-public.");
        ThrowIf(r.IsPublic && r.ClassId.HasValue, "Public exam must not include ClassId.");

        if (r.ClassId.HasValue)
        {
            var cls = await _repo.GetClassByIdAsync(r.ClassId.Value, ct);
            if (cls == null)
            {
                throw new KeyNotFoundException("Class not found.");
            }

            ThrowIf(cls.TeacherId != r.TeacherId, "Class belongs to another teacher.");
            ThrowIf(cls.SubjectId != subjectId, "Subject mismatch.");
        }

        ThrowIf(questionIds.Count == 0, "No questions selected.");

        using var tx = await _repo.BeginTransactionAsync(ct);
        try
        {
            var exam = new Exam
            {
                ExamBlueprintId = blueprintId,
                TeacherId = r.TeacherId,
                ClassId = r.ClassId,
                Title = r.Title,
                SubjectId = subjectId,
                Description = r.Description,
                Duration = r.Duration,
                ShowScore = r.ShowScore,
                ShowAnswer = r.ShowAnswer,
                MaxAttempts = r.MaxAttempts,
                VisibleFrom = r.VisibleFrom,
                OpenAt = r.OpenAt,
                CloseAt = r.CloseAt,
                ShuffleQuestion = r.ShuffleQuestion,
                AllowLateSubmission = r.AllowLateSubmission,
                Status = ExamStatus.Ready,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await _repo.SaveExamAsync(exam, ct);

            var createdPapers = new List<CreatedPaperDto>();
            int startCode = r.PaperCode > 0 ? r.PaperCode : 1;

            for (int i = 0; i < r.PaperCount; i++)
            {
                var paper = new Paper
                {
                    ExamId = exam.ExamId,
                    Code = startCode + i
                };

                await _repo.SavePaperAsync(paper, ct);

                var orderedQuestionIds = r.ShuffleQuestion
                    ? ShuffleQuestionIds(questionIds)
                    : [.. questionIds];

                await _repo.AddPaperQuestionsAsync(paper.PaperId, orderedQuestionIds, ct);

                createdPapers.Add(new CreatedPaperDto(paper.PaperId, paper.Code));
            }

            await tx.CommitAsync(ct);

            return new CreateAssignExamResponse(
                exam.ExamId,
                createdPapers[0].PaperId,
                questionIds.Count,
                createdPapers
            );
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<ExamReviewDto> GetExamReviewAsync(int id, CancellationToken ct = default)
    {
        var e = await _repo.GetExamReviewDataAsync(id, ct);
        if (e == null)
        {
            throw new KeyNotFoundException("Exam not found.");
        }

        var matrix = (e.ExamBlueprint?.ExamBlueprintChapters ?? [])
            .GroupBy(bc => bc.Chapter?.Name ?? "N/A")
            .Select(g => new BlueprintRowDto
            {
                ChapterName = g.Key,
                Recognize = g.Where(x => x.Difficulty == 1).Sum(x => x.TotalOfQuestions),
                Understand = g.Where(x => x.Difficulty == 2).Sum(x => x.TotalOfQuestions),
                Apply = g.Where(x => x.Difficulty == 3).Sum(x => x.TotalOfQuestions),
                AdvancedApply = g.Where(x => x.Difficulty == 4).Sum(x => x.TotalOfQuestions),
                Total = g.Sum(x => x.TotalOfQuestions)
            })
            .ToList();

        var papers = e.Papers.Select(p => new PaperReviewDto(
            p.PaperId,
            p.Code,
            p.Questions.Select(q => new QuestionReviewDto(
                q.QuestionId,
                q.QuestionType,
                q.QuestionContent,
                q.Difficulty,
                q.Chapter?.Name ?? "N/A"
            )).ToList()
        )).ToList();

        return new ExamReviewDto(
            e.ExamId,
            e.Title,
            e.Subject?.Code ?? "N/A",
            e.Description,
            e.Papers.FirstOrDefault()?.Questions.Count ?? 0,
            e.Duration,
            e.OpenAt,
            e.CloseAt,
            e.Teacher?.FullName ?? "N/A",
            e.UpdatedAtUtc,
            e.Status,
            matrix,
            papers
        );
    }

    public async Task<IReadOnlyList<QuestionListItemDto>> GetAlternativeQuestionsAsync(int pid, int qid, CancellationToken ct = default)
    {
        var p = await _repo.GetPaperWithQuestionsAsync(pid, ct);
        if (p == null) throw new KeyNotFoundException("Paper not found.");

        var old = await _repo.GetQuestionByIdAsync(qid, ct);
        if (old == null) throw new KeyNotFoundException("Question not found.");

        var currentIds = p.Questions.Select(q => q.QuestionId).ToList();

        return await _repo.GetAlternativeQuestionsAsync(
            p.Exam.SubjectId,
            old.Difficulty,
            ActiveStatus,
            currentIds,
            ct);
    }

    public async Task SwapPaperQuestionAsync(SwapQuestionRequestDto r, CancellationToken ct = default)
    {
        var p = await _repo.GetPaperWithQuestionsAsync(r.PaperId, ct);
        if (p == null) throw new KeyNotFoundException("Paper not found.");

        var old = p.Questions.FirstOrDefault(q => q.QuestionId == r.OldQuestionId);
        if (old == null) throw new ArgumentException("Old question not found in this paper.");

        var @new = await _repo.GetQuestionByIdAsync(r.NewQuestionId, ct);
        if (@new == null) throw new KeyNotFoundException("New question not found.");

        ThrowIf(!ActiveStatus.Contains(@new.Status), "New question is inactive.");
        ThrowIf(@new.Difficulty != old.Difficulty, "Difficulty mismatch.");

        await _repo.SwapPaperQuestionAsync(r.PaperId, r.OldQuestionId, r.NewQuestionId, ct);
    }

    public async Task ApproveExamAsync(int id, CancellationToken ct = default)
    {
        await _repo.UpdateExamStatusAsync(id, ExamStatus.Published, ct);
    }

    private async Task<(int SubjId, int? BpId, List<int> QIds)> BuildFromBlueprintAsync(int? id, CancellationToken ct)
    {
        if (!id.HasValue)
        {
            throw new ArgumentException("ExamBlueprintId required.");
        }

        var bp = await _repo.GetBlueprintWithChaptersAsync(id.Value, ct);
        if (bp == null)
        {
            throw new KeyNotFoundException("Blueprint not found.");
        }

        var questionIds = new List<int>();

        foreach (var r in bp.ExamBlueprintChapters)
        {
            var p = await _repo.GetQuestionIdsForBlueprintRowAsync(r.ChapterId, r.Difficulty, r.TotalOfQuestions, ActiveStatus, ct);
            if (p.Count < r.TotalOfQuestions)
            {
                throw new InvalidOperationException($"Not enough questions for chapter {r.ChapterId} with difficulty {r.Difficulty}.");
            }
            questionIds.AddRange(p);
        }

        return (bp.SubjectId, bp.ExamBlueprintId, questionIds);
    }

    private async Task<(int SubjId, int? BpId, List<int> QIds)> BuildFromManualAsync(int? sid, IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        if (ids == null || ids.Count == 0)
        {
            throw new ArgumentException("QuestionIds required.");
        }

        var sel = await _repo.GetQuestionsWithSubjectByIdsAsync(ids, ActiveStatus, ct);
        if (sel.Count != ids.Distinct().Count())
        {
            throw new ArgumentException("One or more invalid or inactive questions.");
        }

        var subjectIds = sel.Select(x => x.SubjectId).Distinct().ToList();
        if (subjectIds.Count != 1)
        {
            throw new ArgumentException("Questions must belong to the same subject.");
        }

        if (sid.HasValue && sid.Value != subjectIds[0])
        {
            throw new ArgumentException("Subject mismatch.");
        }

        return (subjectIds[0], null, sel.Select(x => x.QuestionId).ToList());
    }

    private static List<int> ShuffleQuestionIds(IReadOnlyList<int> s)
    {
        return s.OrderBy(_ => Guid.NewGuid()).ToList();
    }

    private static void ValidateTimeWindow(DateTime? v, DateTime? o, DateTime? c)
    {
        if (v > o)
        {
            throw new ArgumentException("VisibleFrom > OpenAt.");
        }
        if (o >= c)
        {
            throw new ArgumentException("OpenAt >= CloseAt.");
        }
    }
}
