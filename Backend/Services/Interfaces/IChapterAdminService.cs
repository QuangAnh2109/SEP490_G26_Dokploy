using System.Threading.Tasks;
using Backend.Common.Models;
using Backend.DTOs.Curriculum.Chapter;

namespace Backend.Services.Interfaces;

public interface IChapterAdminService
{
    Task<Result<ChapterDetail>> CreateAsync(int subjectId, CreateChapterRequest request, int adminUserId);
    Task<Result<ChapterDetail>> UpdateAsync(int subjectId, int chapterId, UpdateChapterRequest request, int adminUserId);
    Task<Result> DeleteAsync(int subjectId, int chapterId, int adminUserId);
    Task<Result> ReorderAsync(int subjectId, ReorderChaptersRequest request, int adminUserId);
}
