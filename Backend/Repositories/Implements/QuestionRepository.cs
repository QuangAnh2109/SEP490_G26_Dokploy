using Backend.Constants;
using Backend.DTOs.Question;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implements
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly MtcaSep490G26Context _dbContext;

        public QuestionRepository(MtcaSep490G26Context dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(List<QuestionListItemDto> Items, int TotalCount)> GetQuestionsAsync(QuestionListQueryDto query, int userId)
        {
            var q = _dbContext.Questions
                .Include(x => x.Chapter)
                    .ThenInclude(c => c.Subject)
                .Include(x => x.QuestionAnswers)
                .Where(x => x.CreatedByUserId == userId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var kw = query.Keyword.Trim();
                q = q.Where(x => x.QuestionContent.Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(query.QuestionType))
            {
                q = q.Where(x => x.QuestionType == query.QuestionType);
            }

            if (query.Difficulty.HasValue)
            {
                q = q.Where(x => x.Difficulty == query.Difficulty.Value);
            }

            if (query.ChapterId.HasValue)
            {
                q = q.Where(x => x.ChapterId == query.ChapterId.Value);
            }

            if (query.SubjectId.HasValue)
            {
                q = q.Where(x => x.Chapter.SubjectId == query.SubjectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                q = q.Where(x => x.Status == query.Status);
            }

            var totalCount = await q.CountAsync();

            var pageSize = Math.Clamp(query.PageSize, 1, 50);
            var page = Math.Max(query.Page, 1);

            var items = await q
                .OrderByDescending(x => x.UpdatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new QuestionListItemDto
                {
                    QuestionId = x.QuestionId,
                    ContentPreview = x.QuestionContent,
                    QuestionType = x.QuestionType,
                    Difficulty = x.Difficulty,
                    DifficultyLabel = DifficultyLevel.GetLabel(x.Difficulty),
                    SubjectCode = x.Chapter.Subject.Code ?? x.Chapter.Subject.Name,
                    ChapterName = x.Chapter.Name,
                    UpdatedAt = x.UpdatedAtUtc,
                    Status = x.Status,
                    AnswerCount = x.QuestionAnswers.Count
                })
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<Question>> CreateQuestionsAsync(List<Question> questions)
        {
            _dbContext.Questions.AddRange(questions);
            await _dbContext.SaveChangesAsync();
            return questions;
        }

        public async Task<List<InputType>> GetInputTypesAsync()
        {
            return await _dbContext.InputTypes.OrderBy(x => x.GroupType).ThenBy(x => x.Name).ToListAsync();
        }

        public async Task<List<Subject>> GetSubjectsWithChaptersAsync()
        {
            return await _dbContext.Subjects
                .Include(s => s.Chapters)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<bool> ChapterExistsAsync(int chapterId)
        {
            return await _dbContext.Chapters.AnyAsync(c => c.ChapterId == chapterId);
        }

        public async Task<bool> InputTypeExistsAsync(int inputTypeId)
        {
            return await _dbContext.InputTypes.AnyAsync(it => it.InputTypeId == inputTypeId);
        }

        public async Task<GroupAnswer> CreateGroupAnswerAsync(GroupAnswer groupAnswer)
        {
            _dbContext.GroupAnswers.Add(groupAnswer);
            await _dbContext.SaveChangesAsync();
            return groupAnswer;
        }
    }
}
