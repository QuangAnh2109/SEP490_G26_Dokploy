using System;
using System.Threading.Tasks;
using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Constants;
using Backend.DTOs.Curriculum.Chapter;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services.Implements;

public class ChapterAdminService : IChapterAdminService
{
    private readonly IChapterAdminRepository _chapterRepo;
    private readonly TimeProvider _timeProvider;

    public ChapterAdminService(
        IChapterAdminRepository chapterRepo,
        TimeProvider timeProvider)
    {
        _chapterRepo = chapterRepo;
        _timeProvider = timeProvider;
    }

    private async Task<Result?> GuardSubjectActiveAsync(int subjectId)
    {
        var subject = await _chapterRepo.GetSubjectStatusAsync(subjectId);
        if (subject is null) return SubjectErrors.NotFound;
        if (subject.Status == SubjectStatus.Closed) return SubjectErrors.Closed;
        return null;
    }

    private ChapterDetail MapToDetail(Chapter c)
    {
        return new ChapterDetail
        {
            ChapterId = c.ChapterId,
            Name = c.Name,
            Description = c.Description,
            DisplayOrder = c.DisplayOrder,
            Status = c.Status,
            QuestionCount = 0, // D4 explicitly says no need to check or return questions directly here, we just use 0 if not mapped.
            UpdatedByName = "", // We can leave it blank since repo doesn't Include UpdatedByUser for performance, or fetch it. Plan says "ChapterListItem reuse". We'll just return what's available.
            UpdatedAtUtc = c.UpdatedAtUtc,
            CreatedByName = "",
            CreatedAtUtc = c.CreatedAtUtc,
            ConcurrencyStamp = Convert.ToBase64String(c.ConcurrencyStamp)
        };
    }

    public async Task<Result<ChapterDetail>> CreateAsync(int subjectId, CreateChapterRequest request, int adminUserId)
    {
        if (await GuardSubjectActiveAsync(subjectId) is { } guardErr) 
            return Result<ChapterDetail>.Failure(guardErr.Error ?? Error.None);

        var existing = await _chapterRepo.GetByNameAsync(subjectId, request.Name);
        if (existing != null)
            return ChapterErrors.NameDuplicate;

        var displayOrder = request.DisplayOrder ?? (await _chapterRepo.GetMaxDisplayOrderAsync(subjectId)) + 1;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var chapter = new Chapter
        {
            SubjectId = subjectId,
            Name = request.Name,
            Description = request.Description,
            DisplayOrder = displayOrder,
            Status = ChapterStatus.Active,
            CreatedByUserId = adminUserId,
            CreatedAtUtc = now,
            UpdatedByUserId = adminUserId,
            UpdatedAtUtc = now
        };

        await _chapterRepo.AddAsync(chapter);
        await _chapterRepo.SaveChangesAsync();

        return Result<ChapterDetail>.Success(MapToDetail(chapter));
    }

    public async Task<Result<ChapterDetail>> UpdateAsync(int subjectId, int chapterId, UpdateChapterRequest request, int adminUserId)
    {
        if (await GuardSubjectActiveAsync(subjectId) is { } guardErr) 
            return Result<ChapterDetail>.Failure(guardErr.Error ?? Error.None);

        var chapter = await _chapterRepo.GetByIdAsync(subjectId, chapterId);
        if (chapter == null)
            return ChapterErrors.NotFound;

        var currentStamp = Convert.ToBase64String(chapter.ConcurrencyStamp);
        if (currentStamp != request.ConcurrencyStamp)
            return ChapterErrors.ConcurrentUpdate;

        if (chapter.Name != request.Name)
        {
            var existing = await _chapterRepo.GetByNameAsync(subjectId, request.Name);
            if (existing != null)
                return ChapterErrors.NameDuplicate;
        }

        chapter.Name = request.Name;
        chapter.Description = request.Description;
        
        if (request.DisplayOrder.HasValue)
        {
            chapter.DisplayOrder = request.DisplayOrder.Value;
        }

        chapter.UpdatedByUserId = adminUserId;
        chapter.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _chapterRepo.UpdateAsync(chapter);
        await _chapterRepo.SaveChangesAsync();

        return Result<ChapterDetail>.Success(MapToDetail(chapter));
    }

    public async Task<Result> DeleteAsync(int subjectId, int chapterId, int adminUserId)
    {
        if (await GuardSubjectActiveAsync(subjectId) is { } guardErr) 
            return guardErr;

        var chapter = await _chapterRepo.GetByIdAsync(subjectId, chapterId);
        if (chapter == null)
            return ChapterErrors.NotFound;

        if (chapter.Status == ChapterStatus.Deleted)
            return ChapterErrors.AlreadyDeleted;

        chapter.Status = ChapterStatus.Deleted;
        chapter.UpdatedByUserId = adminUserId;
        chapter.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _chapterRepo.UpdateAsync(chapter);
        await _chapterRepo.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ReorderAsync(int subjectId, ReorderChaptersRequest request, int adminUserId)
    {
        if (await GuardSubjectActiveAsync(subjectId) is { } guardErr) 
            return guardErr;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var updated = await _chapterRepo.BulkReorderAsync(subjectId, request.Items, adminUserId, now);
        
        if (!updated)
            return ChapterErrors.NotFound;

        return Result.Success();
    }
}
