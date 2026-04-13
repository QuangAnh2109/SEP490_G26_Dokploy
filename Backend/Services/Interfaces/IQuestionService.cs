using Backend.DTOs.Question;

namespace Backend.Services.Interfaces
{
    public interface IQuestionService
    {
        Task<QuestionListResultDto> GetQuestionsAsync(QuestionListQueryDto query, int userId);
        Task<QuestionDto> GetQuestionByIdAsync(int questionId, int userId);
        Task<List<QuestionSummaryDto>> CreateQuestionsAsync(int userId, List<QuestionDto> request);
        Task<QuestionSummaryDto> UpdateQuestionAsync(int questionId, int userId, QuestionDto request);
        Task<int> UpdateQuestionStatusAsync(List<int> questionIds, int userId, string status);
        Task DeleteQuestionAsync(int questionId, int userId);
        Task<QuestionMetadataDto> GetQuestionMetadataAsync();
    }
}
