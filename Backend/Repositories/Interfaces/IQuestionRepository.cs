using Backend.DTOs.Question;
using Backend.Models;

namespace Backend.Repositories.Interfaces
{
    public interface IQuestionRepository
    {
        Task<(List<QuestionSummaryDto> Items, int TotalCount)> GetQuestionsAsync(QuestionListQueryDto query, int userId);
        Task<List<Question>> CreateQuestionsAsync(List<Question> questions);
        Task<List<InputType>> GetInputTypesAsync();
        Task<List<Subject>> GetSubjectsWithChaptersAsync();
        Task<bool> ChapterExistsAsync(int chapterId);
        Task<bool> InputTypeExistsAsync(int inputTypeId);
        Task<GroupAnswer> CreateGroupAnswerAsync(GroupAnswer groupAnswer);
        Task DeleteGroupAnswersAsync(IEnumerable<GroupAnswer> groupAnswers);
        Task<Question?> GetQuestionWithAnswersAsync(int id);
        Task<List<Question>> GetQuestionsByIdsAsync(IEnumerable<int> ids);
        Task DeleteQuestionAnswersAsync(IEnumerable<QuestionAnswer> answers);
        Task DeleteQuestionAsync(Question question);
        Task<bool> IsQuestionUsedAsync(int questionId);
        Task SaveChangesAsync();
    }
}
