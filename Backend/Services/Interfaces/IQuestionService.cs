using Backend.Common;
using Backend.Common.Models;
using Backend.DTOs.Question;

namespace Backend.Services.Interfaces
{
    public interface IQuestionService
    {
        Task<Result<QuestionListResultDto>> GetQuestionsAsync(QuestionListQueryDto query);
        Task<Result<QuestionDto>> GetQuestionByIdAsync(int questionId);
        Task<Result<List<QuestionSummaryDto>>> CreateQuestionsAsync(List<QuestionDto> request);
        Task<Result<QuestionSummaryDto>> UpdateQuestionAsync(int questionId, QuestionDto request);
        Task<Result<int>> UpdateQuestionStatusAsync(List<int> questionIds, string status);
        Task<Result> DeleteQuestionAsync(int questionId);
        Task<Result<QuestionMetadataDto>> GetQuestionMetadataAsync();
    }
}
