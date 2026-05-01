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
        private readonly TimeProvider _timeProvider;

        public QuestionRepository(MtcaSep490G26Context dbContext, TimeProvider timeProvider)
        {
            _dbContext = dbContext;
            _timeProvider = timeProvider;
        }

        public async Task<(List<QuestionSummaryDto> Items, int TotalCount)> GetQuestionsAsync(QuestionListQueryDto queryDto, int userId)
        {
            var query = _dbContext.Questions
                .Include(x => x.Chapter)
                    .ThenInclude(c => c.Subject)
                .Include(x => x.QuestionAnswers)
                .Where(x => x.CreatedByUserId == userId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(queryDto.Keyword))
            {
                var kw = queryDto.Keyword.Trim();
                query = query.Where(x => x.QuestionContent.Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(queryDto.QuestionType))
            {
                query = query.Where(x => x.QuestionType == queryDto.QuestionType);
            }

            if (queryDto.Difficulty.HasValue)
            {
                query = query.Where(x => x.Difficulty == queryDto.Difficulty.Value);
            }

            if (queryDto.ChapterId.HasValue)
            {
                query = query.Where(x => x.ChapterId == queryDto.ChapterId.Value);
            }

            if (queryDto.SubjectId.HasValue)
            {
                query = query.Where(x => x.Chapter.SubjectId == queryDto.SubjectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(queryDto.Status))
            {
                query = query.Where(x => x.Status == queryDto.Status);
            }

            if (queryDto.QuestionPurpose.HasValue)
            {
                query = query.Where(x => x.QuestionPurpose == queryDto.QuestionPurpose.Value);
            }

            var totalCount = await query.CountAsync();

            var pageSize = 10;
            var page = Math.Max(queryDto.Page, 1);

            var items = await query
                .OrderByDescending(x => x.UpdatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new QuestionSummaryDto
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
                    QuestionPurpose = x.QuestionPurpose,
                    AnswerCount = x.QuestionAnswers.Count
                })
                .ToListAsync();

            // Compute labels after materialization (can't use custom methods in LINQ-to-SQL)
            foreach (var item in items)
            {
                item.QuestionPurposeLabel = QuestionPurpose.GetLabel(item.QuestionPurpose);
            }

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

        public async Task<Question?> GetQuestionWithAnswersAsync(int id)
        {
            return await _dbContext.Questions
                .Include(q => q.QuestionAnswers)
                    .ThenInclude(qa => qa.BlankInputs)
                .Include(q => q.QuestionAnswers)
                    .ThenInclude(qa => qa.GroupAnswer)
                .FirstOrDefaultAsync(q => q.QuestionId == id);
        }

        public Task DeleteGroupAnswersAsync(IEnumerable<GroupAnswer> groupAnswers)
        {
            _dbContext.GroupAnswers.RemoveRange(groupAnswers);
            return Task.CompletedTask;
        }

        public Task DeleteQuestionAnswersAsync(IEnumerable<QuestionAnswer> answers)
        {
            _dbContext.QuestionAnswers.RemoveRange(answers);
            return Task.CompletedTask;
        }

        public async Task<List<Question>> GetQuestionsByIdsAsync(IEnumerable<int> ids)
        {
            return await _dbContext.Questions
                .Where(q => ids.Contains(q.QuestionId))
                .ToListAsync();
        }

        public Task DeleteQuestionAsync(Question question)
        {
            _dbContext.Questions.Remove(question);
            return Task.CompletedTask;
        }

        public async Task<bool> IsQuestionUsedAsync(int questionId)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            // 1. Any actual student answers? (Highest priority)
            var hasAnswered = await _dbContext.QuestionAnswers
                .Where(qa => qa.QuestionId == questionId)
                .AnyAsync(qa => qa.StudentAnswers.Any());

            if (hasAnswered) return true;

            // 2. Is it in any exam that is already "visible" or "open"?
            // We check both VisibleFrom and OpenAt. If either is in the past, students are seeing it.
            return await _dbContext.Questions
                .Where(q => q.QuestionId == questionId)
                .AnyAsync(q => q.Papers.Any(p =>
                    p.Exam != null && (
                    (p.Exam.VisibleFrom != null && p.Exam.VisibleFrom <= now) || 
                    (p.Exam.OpenAt != null && p.Exam.OpenAt <= now))
                ));
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
