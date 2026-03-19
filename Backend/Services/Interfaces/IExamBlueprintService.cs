using Backend.DTOs.ExamBlueprint;

namespace Backend.Services.Interfaces
{
    public interface IExamBlueprintService
    {
        Task<List<SubjectOptionDto>> GetSubjectsAsync();
        Task<List<ChapterOptionDto>> GetChaptersBySubjectAsync(int subjectId);
        Task<BlueprintListResponseDto> GetBlueprintsAsync(BlueprintListQueryDto query, int currentUserId);
        Task<BlueprintDetailDto> GetBlueprintDetailAsync(int id, int currentUserId);
        Task<CreateExamBlueprintResponse> CreateBlueprintAsync(int currentUserId, CreateExamBlueprintRequest request);
        Task<CreateExamBlueprintResponse> UpdateBlueprintAsync(int id, int currentUserId, CreateExamBlueprintRequest request);
        Task<int> UpdateBlueprintStatusAsync(IEnumerable<int> examBlueprintIds, int currentUserId, int status);
    }
}
