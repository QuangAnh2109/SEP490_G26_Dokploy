using Backend.DTOs;

namespace Backend.Services.Interfaces;

public interface IAssignExamService
{
    Task<AssignExamFiltersResponseDto> GetFiltersAsync(
        int? teacherId,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<ClassListItemDto>> GetClassesAsync(
        int? teacherId,
        string? keyword,
        string? subjectCode,
        string? semester,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlueprintListItemDto>> GetBlueprintsAsync(
        int? teacherId,
        string? subjectCode,
        string? keyword,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlueprintDetailRowDto>> GetBlueprintDetailAsync(
        int blueprintId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuestionListItemDto>> GetQuestionsAsync(
        int? teacherId,
        string? subjectCode,
        int? chapterId,
        int? difficulty,
        CancellationToken cancellationToken = default);

    Task<CreateAssignExamResponse> CreateAssignExamAsync(
        CreateAssignExamRequest request,
        CancellationToken cancellationToken = default);

    Task<ExamReviewDto> GetExamReviewAsync(int examId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuestionListItemDto>> GetAlternativeQuestionsAsync(int paperId, int questionId, CancellationToken cancellationToken = default);

    Task SwapPaperQuestionAsync(SwapQuestionRequestDto request, CancellationToken cancellationToken = default);

    Task ApproveExamAsync(int examId, CancellationToken cancellationToken = default);
}
