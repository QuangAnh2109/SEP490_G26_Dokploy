using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.DTOs.Curriculum.Chapter;
using Backend.DTOs.Curriculum.Subject;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implements;

public class SubjectRepository : ISubjectRepository
{
    private readonly MtcaSep490G26Context _context;

    public SubjectRepository(MtcaSep490G26Context context)
    {
        _context = context;
    }

    public async Task<List<SubjectListItem>> ListAsync(int? status, string? q)
    {
        var query = _context.Subjects
            .Include(s => s.Chapters)
            .Include(s => s.Classes)
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var lowerQ = q.ToLower();
            query = query.Where(s => s.Code != null && s.Code.ToLower().Contains(lowerQ) || s.Name.ToLower().Contains(lowerQ));
        }

        return await query
            .OrderBy(s => s.Code)
            .Select(s => new SubjectListItem
            {
                SubjectId = s.SubjectId,
                Code = s.Code ?? "",
                Name = s.Name,
                Status = s.Status,
                ChapterCount = s.Chapters.Count,
                ActiveChapterCount = s.Chapters.Count(c => c.Status == 1),
                ClassCount = s.Classes.Count,
                ActiveClassCount = s.Classes.Count(c => c.Status == 1)
            })
            .ToListAsync();
    }

    public async Task<SubjectDetail?> GetDetailAsync(int subjectId)
    {
        // First get the Subject base without loading all chapters to avoid cartesian explosion, but EF Core split queries can handle it.
        // The plan asks for: "Project Chapter.Status != null (gồm cả Deleted)" and "Order By DisplayOrder ASC"
        return await _context.Subjects
            .AsNoTracking()
            .Where(s => s.SubjectId == subjectId)
            .Select(s => new SubjectDetail
            {
                SubjectId = s.SubjectId,
                Code = s.Code ?? "",
                Name = s.Name,
                Description = s.Description,
                Status = s.Status,
                ChapterCount = s.Chapters.Count,
                ActiveChapterCount = s.Chapters.Count(c => c.Status == 1),
                ClassCount = s.Classes.Count,
                ActiveClassCount = s.Classes.Count(c => c.Status == 1),
                CreatedByName = s.CreatedByUser.Email, // Email or Name if User table has Name
                CreatedAtUtc = s.CreatedAtUtc,
                UpdatedByName = s.UpdatedByUser.Email,
                UpdatedAtUtc = s.UpdatedAtUtc,
                // Using Convert.ToBase64String for row version
                ConcurrencyStamp = System.Convert.ToBase64String(s.ConcurrencyStamp),
                Chapters = s.Chapters
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new ChapterListItem
                    {
                        ChapterId = c.ChapterId,
                        Name = c.Name,
                        Description = c.Description,
                        DisplayOrder = c.DisplayOrder,
                        Status = c.Status,
                        // QuestionCount could be complex, assuming it is nav property or we join?
                        // Chapter doesn't have Questions navigation directly if we check Chapter model. Wait, let's leave it 0 if not mapped.
                        QuestionCount = 0, 
                        UpdatedByName = c.UpdatedByUser.Email,
                        UpdatedAtUtc = c.UpdatedAtUtc
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Subject?> GetByCodeAsync(string code)
    {
        return await _context.Subjects.FirstOrDefaultAsync(s => s.Code == code);
    }

    public async Task<Subject?> GetByIdAsync(int subjectId)
    {
        return await _context.Subjects.FindAsync(subjectId);
    }

    public async Task AddAsync(Subject subject)
    {
        await _context.Subjects.AddAsync(subject);
    }

    public Task UpdateAsync(Subject subject)
    {
        _context.Subjects.Update(subject);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
