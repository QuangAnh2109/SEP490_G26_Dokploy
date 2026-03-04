using Backend.DTOs.Question;

namespace Backend.Services.Interfaces
{
    public interface IQuestionService
    {
        Task<QuestionListResponseDto> GetQuestionsAsync(QuestionListQueryDto query, int userId);
        Task<CreateQuestionBatchResponse> CreateQuestionsAsync(int userId, CreateQuestionBatchRequest request);
        Task<List<InputTypeDto>> GetInputTypesAsync();
        Task<List<SubjectWithChaptersDto>> GetSubjectsWithChaptersAsync();
    }
}
