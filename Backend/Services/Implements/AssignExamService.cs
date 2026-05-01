using Backend.DTOs;
using Backend.Constants;
using Backend.Jobs;
using Backend.Models;
using Backend.Services.Interfaces;
using Backend.Repositories.Interfaces;
using Backend.Common.Models;
using Backend.Common.Errors;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Backend.Services.Implements;

public class AssignExamService(
    IAssignExamRepository repo,
    ICurrentUserService currentUserService,
    IExamStatusScheduler examStatusScheduler,
    TimeProvider timeProvider) : IAssignExamService
{
    private static readonly string[] ActiveStatus = [QuestionStatus.Active, QuestionStatus.Inprogress];

    private readonly IAssignExamRepository _repo = repo;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IExamStatusScheduler _examStatusScheduler = examStatusScheduler;
    private readonly TimeProvider _timeProvider = timeProvider;

    private async Task<Result> EnsureUserActiveAsync(int id, CancellationToken ct)
    {
        bool isActive = await _repo.IsUserActiveAsync(id, ct);
        if (!isActive)
        {
            return AssignExamErrors.TeacherNotFound;
        }
        return Result.Success();
    }

    private static string Clean(string? s)
    {
        return s?.Trim() ?? string.Empty;
    }

    public async Task<Result<IReadOnlyList<BlueprintListItemDto>>> GetBlueprintsAsync(
        string? subj, string? kw, CancellationToken ct = default)
    {
        int teacherId = _currentUserService.UserId;
        var userCheck = await EnsureUserActiveAsync(teacherId, ct);
        if (userCheck.IsFailure) return userCheck.Error!;

        subj = subj?.Trim();
        kw = kw?.Trim();

        var res = await _repo.GetBlueprintsAsync(teacherId, subj, kw, ct);
        return Result<IReadOnlyList<BlueprintListItemDto>>.Success(res);
    }

    public async Task<Result<IReadOnlyList<BlueprintDetailRowDto>>> GetBlueprintDetailAsync(int id, CancellationToken ct = default)
    {
        var res = await _repo.GetBlueprintDetailAsync(id, ct);
        return Result<IReadOnlyList<BlueprintDetailRowDto>>.Success(res);
    }

    public async Task<Result<IReadOnlyList<QuestionListItemDto>>> GetQuestionsAsync(
        string? subj, int? ch, int? diff, CancellationToken ct = default)
    {
        int teacherId = _currentUserService.UserId;
        var userCheck = await EnsureUserActiveAsync(teacherId, ct);
        if (userCheck.IsFailure) return userCheck.Error!;

        subj = subj?.Trim();
        var res = await _repo.GetQuestionsAsync(teacherId, subj, ch, diff, ActiveStatus, ct);
        return Result<IReadOnlyList<QuestionListItemDto>>.Success(res);
    }

    public async Task<Result<CreateAssignExamResponse>> CreateAssignExamAsync(CreateAssignExamRequest r, CancellationToken ct = default)
    {
        var valResult = ValidateTimeWindow(r.VisibleFrom, r.OpenAt, r.CloseAt);
        if (valResult.IsFailure) return valResult.Error!;

        int teacherId = _currentUserService.UserId;
        var userCheck = await EnsureUserActiveAsync(teacherId, ct);
        if (userCheck.IsFailure) return userCheck.Error!;

        if (r.IsPublic == false && !r.ClassId.HasValue) return AssignExamErrors.MissingClassId;
        if (r.IsPublic == true && r.ClassId.HasValue) return AssignExamErrors.PublicWithClassId;

        string generationMode = Clean(r.GenerationMode).ToLower();
        var mode = generationMode == "manual" ? "manual" : "blueprint";

        var papersQuestions = new List<List<int>>();
        int paperCount = r.PaperCount ?? 1;
        for (int i = 0; i < paperCount; i++) papersQuestions.Add([]);

        int subjectId;
        int? blueprintId;

        if (mode == "blueprint")
        {
            var bp = await _repo.GetBlueprintWithChaptersAsync(r.ExamBlueprintId ?? 0, ct);
            if (bp == null) return AssignExamErrors.BlueprintNotFound;
            
            subjectId = bp.SubjectId;
            blueprintId = bp.ExamBlueprintId;

            foreach (var row in bp.ExamBlueprintChapters)
            {
                var bank = await _repo.GetAllQuestionIdsForBlueprintRowAsync(row.ChapterId, row.Difficulty, ActiveStatus, ct);
                if (bank.Count < row.TotalOfQuestions)
                {
                    return AssignExamErrors.InsufficientQuestions;
                }

                var shuffledBank = bank.OrderBy(_ => Guid.NewGuid()).ToList();
                int bankIdx = 0;

                for (int i = 0; i < paperCount; i++)
                {
                    var paperSet = new List<int>();
                    int neededQuestions = row.TotalOfQuestions;

                    if (bankIdx + neededQuestions > shuffledBank.Count)
                    {
                        var firstPart = shuffledBank.GetRange(bankIdx, shuffledBank.Count - bankIdx);
                        paperSet.AddRange(firstPart);
                        int remainingCount = neededQuestions - firstPart.Count;

                        shuffledBank = bank.OrderBy(_ => Guid.NewGuid()).ToList();
                        
                        var nextPart = shuffledBank.Except(firstPart).Take(remainingCount).ToList();
                        paperSet.AddRange(nextPart);

                        bankIdx = remainingCount;
                    }
                    else
                    {
                        paperSet.AddRange(shuffledBank.GetRange(bankIdx, neededQuestions));
                        bankIdx += neededQuestions;
                    }

                    papersQuestions[i].AddRange(paperSet);
                }
            }
        }
        else
        {
            var res = await BuildFromManualAsync(r.SubjectId, r.QuestionIds ?? [], ct);
            if (res.IsFailure) return res.Error!;

            subjectId = res.Value.SubjId;
            blueprintId = res.Value.BpId;
            var pool = res.Value.QIds;

            if (r.ShuffleQuestion == true)
            {
                var shuffledPool = pool.OrderBy(_ => Guid.NewGuid()).ToList();
                int poolIdx = 0;

                for (int i = 0; i < paperCount; i++)
                {
                    if (poolIdx + pool.Count > shuffledPool.Count)
                    {
                        shuffledPool = pool.OrderBy(_ => Guid.NewGuid()).ToList();
                        poolIdx = 0;
                    }

                    papersQuestions[i].AddRange(shuffledPool.GetRange(poolIdx, pool.Count));
                    poolIdx += pool.Count;
                }
            }
            else
            {
                for (int i = 0; i < paperCount; i++)
                {
                    papersQuestions[i].AddRange(pool);
                }
            }
        }

        if (r.ShuffleQuestion == true)
        {
            for (int i = 0; i < paperCount; i++)
            {
                papersQuestions[i] = papersQuestions[i].OrderBy(_ => Guid.NewGuid()).ToList();
            }
        }

        if (r.ClassId.HasValue)
        {
            var cls = await _repo.GetClassByIdAsync(r.ClassId.Value, ct);
            if (cls == null) return AssignExamErrors.ClassNotFound;

            if (cls.TeacherId != teacherId) return AssignExamErrors.ClassNotOwnedByTeacher;
            if (cls.SubjectId != subjectId) return AssignExamErrors.SubjectMismatch;
        }

        if (papersQuestions[0].Count == 0) return AssignExamErrors.NoQuestionsSelected;

        using var tx = await _repo.BeginTransactionAsync(ct);
        try
        {
            var exam = new Exam
            {
                ExamBlueprintId = blueprintId,
                TeacherId = teacherId,
                ClassId = r.ClassId,
                Title = r.Title ?? "Untitled",
                SubjectId = subjectId,
                Description = r.Description,
                Duration = r.Duration ?? 0,
                ShowScore = r.ShowScore ?? 0,
                ShowAnswer = r.ShowAnswer ?? 0,
                AnswerTimingMode = r.AnswerTimingMode ?? 0,
                MaxAttempts = r.MaxAttempts ?? 1,
                VisibleFrom = r.VisibleFrom,
                OpenAt = r.OpenAt,
                CloseAt = r.CloseAt,
                ShuffleQuestion = r.ShuffleQuestion ?? false,
                Status = ExamStatus.Ready,
                UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
            };

            await _repo.SaveExamAsync(exam, ct);

            var createdPapers = new List<CreatedPaperDto>();
            int startCode = (r.PaperCode > 0 ? r.PaperCode : 1) ?? 1;

            for (int i = 0; i < paperCount; i++)
            {
                var paper = new Paper
                {
                    ExamId = exam.ExamId,
                    Code = startCode + i
                };

                await _repo.SavePaperAsync(paper, ct);

                var orderedQuestionIds = papersQuestions[i];

                await _repo.AddPaperQuestionsAsync(paper.PaperId, orderedQuestionIds, ct);

                createdPapers.Add(new CreatedPaperDto(paper.PaperId, paper.Code ?? 0));
            }

            await tx.CommitAsync(ct);

            return Result<CreateAssignExamResponse>.Success(new CreateAssignExamResponse(
                exam.ExamId,
                createdPapers[0].PaperId,
                papersQuestions[0].Count,
                createdPapers
            ));
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<Result<ExamReviewDto>> GetExamReviewAsync(int id, CancellationToken ct = default)
    {
        var exam = await _repo.GetExamReviewDataAsync(id, ct);
        if (exam == null) return AssignExamErrors.ExamNotFound;

        var matrix = (exam.ExamBlueprint?.ExamBlueprintChapters ?? [])
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

        var papers = exam.Papers.Select(p => new PaperReviewDto(
            p.PaperId,
            p.Code ?? 0,
            p.Questions.Select(q => new QuestionReviewDto(
                q.QuestionId,
                q.QuestionType,
                q.QuestionContent,
                q.Difficulty,
                q.Chapter?.Name ?? "N/A",
                q.QuestionAnswers.Select(a => new QuestionReviewAnswerDto(
                    a.QuestionAnswerId,
                    a.Content,
                    a.CorrectAnswer,
                    a.IsCorrect ?? false
                )).ToList()
            )).ToList()
        )).ToList();

        return Result<ExamReviewDto>.Success(new ExamReviewDto(
            exam.ExamId,
            exam.ClassId,
            exam.Title,
            exam.Subject?.Code ?? "N/A",
            exam.Description,
            exam.Papers.FirstOrDefault()?.Questions.Count ?? 0,
            exam.Duration,
            exam.VisibleFrom,
            exam.OpenAt,
            exam.CloseAt,
            exam.Teacher?.FullName ?? "N/A",
            exam.UpdatedAtUtc,
            exam.Status,
            matrix,
            papers
        ));
    }

    public async Task<Result<IReadOnlyList<QuestionListItemDto>>> GetAlternativeQuestionsAsync(int pid, int qid, CancellationToken ct = default)
    {
        var paper = await _repo.GetPaperWithQuestionsAsync(pid, ct);
        if (paper == null) return AssignExamErrors.PaperNotFound;

        var old = await _repo.GetQuestionByIdAsync(qid, ct);
        if (old == null) return AssignExamErrors.QuestionNotFound;

        var currentIds = paper.Questions.Select(q => q.QuestionId).ToList();

        if (paper.Exam == null) return AssignExamErrors.ExamNotFound;

        var res = await _repo.GetAlternativeQuestionsAsync(
            paper.Exam.SubjectId,
            old.ChapterId,
            old.Difficulty,
            ActiveStatus,
            currentIds,
            ct);
        
        return Result<IReadOnlyList<QuestionListItemDto>>.Success(res);
    }

    public async Task<Result> SwapPaperQuestionAsync(SwapQuestionRequestDto r, CancellationToken ct = default)
    {
        if (!r.PaperId.HasValue || !r.OldQuestionId.HasValue || !r.NewQuestionId.HasValue)
            return AssignExamErrors.SwapMissingFields;

        var paper = await _repo.GetPaperWithQuestionsAsync(r.PaperId.Value, ct);
        if (paper == null) return AssignExamErrors.PaperNotFound;

        var old = paper.Questions.FirstOrDefault(q => q.QuestionId == r.OldQuestionId.Value);
        if (old == null) return AssignExamErrors.QuestionNotInPaper;

        var @new = await _repo.GetQuestionByIdAsync(r.NewQuestionId.Value, ct);
        if (@new == null) return AssignExamErrors.QuestionNotFound;

        if (!ActiveStatus.Contains(@new.Status)) return AssignExamErrors.QuestionInactive;
        if (@new.Difficulty != old.Difficulty) return AssignExamErrors.DifficultyMismatch;
        if (@new.ChapterId != old.ChapterId) return AssignExamErrors.ChapterMismatch;

        if (r.SwapGlobal == true)
        {
            await _repo.SwapExamQuestionGloballyAsync(paper.ExamId ?? 0, r.OldQuestionId.Value, r.NewQuestionId.Value, ct);
        }
        else
        {
            await _repo.SwapPaperQuestionAsync(r.PaperId.Value, r.OldQuestionId.Value, r.NewQuestionId.Value, ct);
        }

        return Result.Success();
    }

    public async Task<Result> ApproveExamAsync(int id, CancellationToken ct = default)
    {
        var exam = await _repo.GetExamByIdAsync(id, ct);
        if (exam == null) return AssignExamErrors.ExamNotFound;

        try
        {
            await _repo.UpdateExamStatusAsync(id, ExamStatus.Published, ct);

            var allQuestionIds = await _repo.GetAllQuestionIdsInExamAsync(id, ct);
            await _repo.UpdateQuestionsToInprogressAsync(allQuestionIds, ct);

            await _repo.UpdateBlueprintToInprogressAsync(id, ct);

            await _examStatusScheduler.ScheduleExamJobsAsync(id, exam.OpenAt, exam.CloseAt, ct);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return AssignExamErrors.ConcurrentUpdate;
        }
    }

    public async Task<Result> CancelExamAsync(int id, CancellationToken ct = default)
    {
        var exam = await _repo.GetExamByIdAsync(id, ct);
        if (exam == null) return AssignExamErrors.ExamNotFound;

        if (exam.Status != ExamStatus.Published)
        {
            return AssignExamErrors.InvalidStatusForCancel;
        }

        bool hasSubmissions = await _repo.HasSubmissionsForExamAsync(id, ct);

        try
        {
            if (hasSubmissions)
            {
                await _repo.UpdateExamStatusAsync(id, ExamStatus.InProgress, ct);
                return AssignExamErrors.ExamAlreadyStarted;
            }

            await _repo.UpdateExamStatusAsync(id, ExamStatus.Cancelled, ct);
            await _examStatusScheduler.CancelExamJobsAsync(id, ct);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return AssignExamErrors.ConcurrentUpdate;
        }
    }

    public async Task<Result> RestoreExamAsync(int id, CancellationToken ct = default)
    {
        var exam = await _repo.GetExamByIdAsync(id, ct);
        if (exam == null) return AssignExamErrors.ExamNotFound;

        if (exam.Status != ExamStatus.Cancelled)
        {
            return AssignExamErrors.InvalidStatusForRestore;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (exam.OpenAt.HasValue && exam.OpenAt.Value <= now)
        {
            return AssignExamErrors.OpenTimePassed;
        }

        if (exam.OpenAt.HasValue && exam.CloseAt.HasValue && exam.Duration > 0)
        {
            var windowMinutes = (exam.CloseAt.Value - exam.OpenAt.Value).TotalMinutes;
            if (windowMinutes < exam.Duration)
            {
                return AssignExamErrors.DurationMismatch;
            }
        }

        try
        {
            await _repo.UpdateExamStatusAsync(id, ExamStatus.Published, ct);
            await _examStatusScheduler.ScheduleExamJobsAsync(id, exam.OpenAt, exam.CloseAt, ct);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return AssignExamErrors.ConcurrentUpdate;
        }
    }

    public async Task<Result> DeleteExamAsync(int id, CancellationToken ct = default)
    {
        var exam = await _repo.GetExamByIdAsync(id, ct);
        if (exam == null) return AssignExamErrors.ExamNotFound;

        if (exam.Status != ExamStatus.Ready && exam.Status != ExamStatus.Cancelled)
        {
            return AssignExamErrors.InvalidStatusForDelete;
        }

        await _repo.HardDeleteExamAsync(id, ct);
        return Result.Success();
    }

    public async Task<Result> UpdateExamInfoAsync(int id, UpdateExamInfoRequest request, CancellationToken ct = default)
    {
        var exam = await _repo.GetExamByIdAsync(id, ct);
        if (exam == null) return AssignExamErrors.ExamNotFound;

        var valResult = ValidateTimeWindow(request.VisibleFrom, request.OpenAt, request.CloseAt);
        if (valResult.IsFailure) return valResult.Error!;

        if (exam.Status != ExamStatus.Cancelled && exam.Status != ExamStatus.Ready)
        {
            return AssignExamErrors.InvalidStatusForUpdate;
        }

        try
        {
            var title = exam.Status == ExamStatus.Cancelled ? null : request.Title;
            await _repo.UpdateExamInfoAsync(id, title, request.VisibleFrom, request.OpenAt, request.CloseAt, ct);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return AssignExamErrors.ConcurrentUpdate;
        }
    }

    private async Task<Result<(int SubjId, int? BpId, List<int> QIds)>> BuildFromManualAsync(int? sid, IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        if (ids == null || ids.Count == 0)
        {
            return AssignExamErrors.ManualEmptyQuestions;
        }

        var sel = await _repo.GetQuestionsWithSubjectByIdsAsync(ids, ActiveStatus, ct);
        if (sel.Count != ids.Distinct().Count())
        {
            return AssignExamErrors.InvalidOrInactiveQuestions;
        }

        var subjectIds = sel.Select(x => x.SubjectId).Distinct().ToList();
        if (subjectIds.Count != 1)
        {
            return AssignExamErrors.MultipleSubjects;
        }

        if (sid.HasValue && sid.Value != subjectIds[0])
        {
            return AssignExamErrors.SubjectMismatch;
        }

        return Result<(int SubjId, int? BpId, List<int> QIds)>.Success((subjectIds[0], null, sel.Select(x => x.QuestionId).ToList()));
    }

    private static Result ValidateTimeWindow(DateTime? v, DateTime? o, DateTime? c)
    {
        if (v > o) return AssignExamErrors.InvalidTimeWindow;
        if (o >= c) return AssignExamErrors.InvalidTimeWindow;
        return Result.Success();
    }
}
