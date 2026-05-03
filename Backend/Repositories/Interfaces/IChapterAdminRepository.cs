using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.DTOs.Curriculum.Chapter;
using Backend.Models;

namespace Backend.Repositories.Interfaces;

public interface IChapterAdminRepository
{
    Task<Subject?> GetSubjectStatusAsync(int subjectId);
    Task<Chapter?> GetByIdAsync(int subjectId, int chapterId);
    Task<Chapter?> GetByNameAsync(int subjectId, string name);
    Task<int> GetMaxDisplayOrderAsync(int subjectId);
    Task AddAsync(Chapter chapter);
    Task UpdateAsync(Chapter chapter);
    Task<bool> BulkReorderAsync(int subjectId, List<ReorderChaptersRequest.ReorderItem> items, int adminUserId, DateTime now);
    Task SaveChangesAsync();
}
