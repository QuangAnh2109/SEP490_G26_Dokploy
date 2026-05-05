using Backend.DTOs.ExamBlueprint;
using Backend.Models;

namespace Backend.Repositories.Interfaces
{
    public interface IExamBlueprintRepository
    {
        Task<List<SubjectOptionDto>> GetSubjectsAsync();
        Task<bool> SubjectExistsAsync(int subjectId);
        Task<List<ChapterOptionDto>> GetChaptersBySubjectAsync(int subjectId);
        Task<(List<BlueprintListItemDto> Items, int TotalCount)> GetBlueprintsAsync(BlueprintListQueryDto query, int currentUserId);
        Task<BlueprintDetailDto?> GetBlueprintDetailAsync(int id, int currentUserId);
        Task<ExamBlueprint> CreateBlueprintAsync(ExamBlueprint blueprint, IEnumerable<ExamBlueprintChapter> rows);
        Task<ExamBlueprint?> UpdateBlueprintAsync(int id, int currentUserId, ExamBlueprint blueprint, IEnumerable<ExamBlueprintChapter> rows);
        Task<int> UpdateBlueprintStatusAsync(IEnumerable<int> examBlueprintIds, int currentUserId, int status);
        Task<bool> IsBlueprintUsedAsync(int blueprintId);
        Task DeleteBlueprintAsync(int blueprintId, int currentUserId);
    }
}
