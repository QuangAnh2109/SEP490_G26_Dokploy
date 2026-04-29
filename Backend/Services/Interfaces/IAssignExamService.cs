using Backend.DTOs;
using Backend.Common.Models;

namespace Backend.Services.Interfaces;

public interface IAssignExamService
{
    Task<Result<IReadOnlyList<BlueprintListItemDto>>> GetBlueprintsAsync(
        string? subjectCode,
        string? keyword,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<BlueprintDetailRowDto>>> GetBlueprintDetailAsync(
        int blueprintId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<QuestionListItemDto>>> GetQuestionsAsync(
        string? subjectCode,
        int? chapterId,
        int? difficulty,
        CancellationToken cancellationToken = default);

    Task<Result<CreateAssignExamResponse>> CreateAssignExamAsync(
        CreateAssignExamRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ExamReviewDto>> GetExamReviewAsync(int examId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<QuestionListItemDto>>> GetAlternativeQuestionsAsync(int paperId, int questionId, CancellationToken cancellationToken = default);

    Task<Result> SwapPaperQuestionAsync(SwapQuestionRequestDto request, CancellationToken cancellationToken = default);

    Task<Result> ApproveExamAsync(int examId, CancellationToken cancellationToken = default);

    Task<Result> CancelExamAsync(int examId, CancellationToken cancellationToken = default);

    Task<Result> RestoreExamAsync(int examId, CancellationToken cancellationToken = default);

    Task<Result> DeleteExamAsync(int examId, CancellationToken cancellationToken = default);

    Task<Result> UpdateExamInfoAsync(int examId, UpdateExamInfoRequest request, CancellationToken cancellationToken = default);
}
