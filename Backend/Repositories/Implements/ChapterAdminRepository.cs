using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.DTOs.Curriculum.Chapter;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implements;

public class ChapterAdminRepository : IChapterAdminRepository
{
    private readonly MtcaSep490G26Context _context;

    public ChapterAdminRepository(MtcaSep490G26Context context)
    {
        _context = context;
    }

    public async Task<Subject?> GetSubjectStatusAsync(int subjectId)
    {
        return await _context.Subjects
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SubjectId == subjectId);
    }

    public async Task<Chapter?> GetByIdAsync(int subjectId, int chapterId)
    {
        return await _context.Chapters
            .FirstOrDefaultAsync(c => c.SubjectId == subjectId && c.ChapterId == chapterId);
    }

    public async Task<Chapter?> GetByNameAsync(int subjectId, string name)
    {
        return await _context.Chapters
            .FirstOrDefaultAsync(c => c.SubjectId == subjectId && c.Name == name);
    }

    public async Task<int> GetMaxDisplayOrderAsync(int subjectId)
    {
        return await _context.Chapters
            .Where(c => c.SubjectId == subjectId)
            .MaxAsync(c => (int?)c.DisplayOrder) ?? 0;
    }

    public async Task AddAsync(Chapter chapter)
    {
        await _context.Chapters.AddAsync(chapter);
    }

    public Task UpdateAsync(Chapter chapter)
    {
        _context.Chapters.Update(chapter);
        return Task.CompletedTask;
    }

    public async Task<bool> BulkReorderAsync(int subjectId, List<ReorderChaptersRequest.ReorderItem> items, int adminUserId, DateTime now)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            var itemIds = items.Select(x => x.ChapterId).ToList();
            var chapters = await _context.Chapters
                .Where(c => c.SubjectId == subjectId && itemIds.Contains(c.ChapterId))
                .ToListAsync();

            if (chapters.Count != items.Count)
            {
                return false;
            }

            var itemDict = items.ToDictionary(x => x.ChapterId, x => x.DisplayOrder);

            foreach (var chapter in chapters)
            {
                if (itemDict.TryGetValue(chapter.ChapterId, out var newOrder))
                {
                    chapter.DisplayOrder = newOrder;
                    chapter.UpdatedByUserId = adminUserId;
                    chapter.UpdatedAtUtc = now;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
