using Backend.Common.Models;
using Backend.DTOs.ExamBlueprint;

namespace Backend.Services.Interfaces
{
    public interface IExamBlueprintService
    {
        Task<Result<List<SubjectOptionDto>>> GetSubjectsAsync();
        Task<Result<List<ChapterOptionDto>>> GetChaptersBySubjectAsync(int subjectId);
        Task<Result<BlueprintListResponseDto>> GetBlueprintsAsync(BlueprintListQueryDto query);
        Task<Result<BlueprintDetailDto>> GetBlueprintDetailAsync(int id);
        Task<Result<CreateExamBlueprintResponse>> CreateBlueprintAsync(CreateExamBlueprintRequest request);
        Task<Result<CreateExamBlueprintResponse>> UpdateBlueprintAsync(int id, CreateExamBlueprintRequest request);
        Task<Result<int>> UpdateBlueprintStatusAsync(IEnumerable<int> examBlueprintIds, int status);
        Task<Result> DeleteBlueprintAsync(int id);
    }
}
