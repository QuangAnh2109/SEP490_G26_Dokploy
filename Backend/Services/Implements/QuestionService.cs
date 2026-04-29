using Backend.Common;
using Backend.Common.Models;
using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs.Question;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Backend.Services.Implements
{
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly ILogger<QuestionService> _logger;
        private readonly ICurrentUserService _currentUserService;

        private static readonly JsonSerializerOptions UnicodeJsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public QuestionService(
            IQuestionRepository questionRepository, 
            ILogger<QuestionService> logger,
            ICurrentUserService currentUserService)
        {
            _questionRepository = questionRepository;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<QuestionListResultDto>> GetQuestionsAsync(QuestionListQueryDto query)
        {
            var userId = _currentUserService.UserId;
            var (items, totalCount) = await _questionRepository.GetQuestionsAsync(query, userId);
            var pageSize = Math.Clamp(query.PageSize, 1, 50);

            return Result<QuestionListResultDto>.Success(new QuestionListResultDto
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                PageSize = pageSize,
                CurrentPage = Math.Max(query.Page, 1)
            });
        }

        public async Task<Result<List<QuestionSummaryDto>>> CreateQuestionsAsync(List<QuestionDto> request)
        {
            var userId = _currentUserService.UserId;
            if (request == null || !request.Any()) return QuestionErrors.EmptyList;

            var created = await _questionRepository.CreateQuestionsAsync(request.Select(q => MapToQuestionEntity(q, userId)).ToList());
            for (int i = 0; i < request.Count; i++)
                await HandleBlankGroupsAsync(created[i], request[i]);

            await _questionRepository.SaveChangesAsync();
            return Result<List<QuestionSummaryDto>>.Success(created.Select(MapToQuestionSummaryDto).ToList());
        }

        public async Task<Result<QuestionSummaryDto>> UpdateQuestionAsync(int questionId, QuestionDto request)
        {
            var userId = _currentUserService.UserId;
            var existing = await _questionRepository.GetQuestionWithAnswersAsync(questionId);
            if (existing == null || existing.CreatedByUserId != userId) return QuestionErrors.NotFound;

            var isUsed = await _questionRepository.IsQuestionUsedAsync(questionId);
            Question result;

            if (existing.Status == QuestionStatus.Inprogress || existing.Status == QuestionStatus.Archive || isUsed)
            {
                existing.Status = QuestionStatus.Archive;
                if (request.Status == QuestionStatus.Inprogress || request.Status == QuestionStatus.Archive) 
                    request.Status = QuestionStatus.Active;
                result = MapToQuestionEntity(request, userId);
                await _questionRepository.CreateQuestionsAsync(new List<Question> { result });
                _logger.LogInformation("Cloned question {OldId} into {NewId} (Used={IsUsed}).", questionId, result.QuestionId, isUsed);
            }
            else
            {
                var incomingIds = request.Answers?.Select(a => a.AnswerId).Where(id => id.HasValue).ToHashSet() ?? new HashSet<int?>();
                var toRemove = existing.QuestionAnswers.Where(a => !incomingIds.Contains(a.QuestionAnswerId)).ToList();
                if (toRemove.Any()) await _questionRepository.DeleteQuestionAnswersAsync(toRemove);
                result = MapToQuestionEntity(request, userId, existing);
            }

            await HandleBlankGroupsAsync(result, request);
            await _questionRepository.SaveChangesAsync();
            return Result<QuestionSummaryDto>.Success(MapToQuestionSummaryDto(result));
        }

        public async Task<Result<QuestionDto>> GetQuestionByIdAsync(int questionId)
        {
            var userId = _currentUserService.UserId;
            var q = await _questionRepository.GetQuestionWithAnswersAsync(questionId);
            if (q == null || q.CreatedByUserId != userId) return QuestionErrors.NotFound;

            var (stem, frame) = ParseContent(q.QuestionContent);
            var dto = new QuestionDto 
            { 
                QuestionType = q.QuestionType, 
                ChapterId = q.ChapterId, 
                Difficulty = q.Difficulty, 
                Status = q.Status, 
                Stem = stem ?? string.Empty, 
                Frame = frame, 
                QuestionPurpose = q.QuestionPurpose 
            };

            dto.Answers = q.QuestionAnswers.Select(a => new AnswerDto {
                AnswerId = a.QuestionAnswerId, Content = a.Content, CorrectAnswer = a.CorrectAnswer, 
                IsCorrect = a.IsCorrect ?? false, Point = a.Point ?? 0, 
                InputTypeId = a.BlankInputs.FirstOrDefault()?.InputTypeId,
                BlankIndex = GetBlankIndex(a.Content)
            }).ToList();

            if (q.QuestionType == QuestionType.FillBlank)
            {
                dto.BlankGroups = q.QuestionAnswers
                    .Where(a => a.GroupAnswer != null)
                    .GroupBy(a => a.GroupAnswerId!.Value)
                    .Select(g => new GroupAnswerDto {
                        GroupAnswerId = g.Key, Name = g.First().GroupAnswer!.Name,
                        DependsOnGroupId = g.First().GroupAnswer!.DependsOnGroupId,
                        BlankIndices = g.Select(a => GetBlankIndex(a.Content) ?? 0).Where(idx => idx > 0).ToList()
                    }).ToList();
            }
            return Result<QuestionDto>.Success(dto);
        }

        public async Task<Result<int>> UpdateQuestionStatusAsync(List<int> questionIds, string status)
        {
            var userId = _currentUserService.UserId;
            var questions = await _questionRepository.GetQuestionsByIdsAsync(questionIds);
            var updatedCount = 0;

            foreach (var q in questions)
            {
                if (q.CreatedByUserId == userId)
                {
                    q.Status = status;
                    q.UpdatedAtUtc = DateTime.UtcNow;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                await _questionRepository.SaveChangesAsync();
                _logger.LogInformation("Updated status of {Count} questions to {Status} (requested {RequestedCount}).", updatedCount, status, questionIds.Count);
            }

            return Result<int>.Success(updatedCount);
        }

        public async Task<Result> DeleteQuestionAsync(int questionId)
        {
            var userId = _currentUserService.UserId;
            var existing = await _questionRepository.GetQuestionWithAnswersAsync(questionId);
            if (existing == null || existing.CreatedByUserId != userId)
            {
                return QuestionErrors.NotFound;
            }

            if (existing.Status != QuestionStatus.Draft && existing.Status != QuestionStatus.Active)
            {
                return QuestionErrors.InvalidDeleteStatus;
            }

            var isUsed = await _questionRepository.IsQuestionUsedAsync(questionId);
            if (isUsed)
            {
                return QuestionErrors.InUse;
            }

            await _questionRepository.DeleteQuestionAsync(existing);
            await _questionRepository.SaveChangesAsync();
            
            _logger.LogInformation("Nhà giáo viên {UserId} đã xóa câu hỏi {QuestionId}.", userId, questionId);
            
            return Result.Success();
        }

        public async Task<Result<QuestionMetadataDto>> GetQuestionMetadataAsync()
        {
            var inputTypes = await _questionRepository.GetInputTypesAsync();
            var subjects = await _questionRepository.GetSubjectsWithChaptersAsync();

            return Result<QuestionMetadataDto>.Success(new QuestionMetadataDto
            {
                InputTypes = inputTypes.Select(it => new InputTypeDto
                {
                    InputTypeId = it.InputTypeId,
                    Name = it.Name,
                    GroupType = it.GroupType
                }).ToList(),
                Subjects = subjects.Select(s => new SubjectWithChaptersDto
                {
                    SubjectId = s.SubjectId,
                    Name = s.Name,
                    Code = s.Code,
                    Chapters = s.Chapters.Select(c => new ChapterDto
                    {
                        ChapterId = c.ChapterId,
                        Name = c.Name
                    }).ToList()
                }).ToList()
            });
        }

        private Question MapToQuestionEntity(QuestionDto item, int userId, Question? existing = null)
        {
            var q = existing ?? new Question { CreatedByUserId = userId };
            q.QuestionType = item.QuestionType ?? string.Empty;
            q.ChapterId = item.ChapterId ?? 0;
            q.Difficulty = item.Difficulty ?? 1;
            q.QuestionPurpose = item.QuestionPurpose ?? 1;
            q.Status = item.Status ?? QuestionStatus.Draft;
            q.UpdatedAtUtc = DateTime.UtcNow;
            q.QuestionContent = JsonSerializer.Serialize(new { stem = item.Stem, frame = item.Frame }, UnicodeJsonOptions);

            if (item.Answers != null)
            {
                foreach (var adto in item.Answers)
                {
                    var ans = adto.AnswerId.HasValue ? q.QuestionAnswers.FirstOrDefault(a => a.QuestionAnswerId == adto.AnswerId) : null;
                    if (ans == null && q.QuestionType == QuestionType.FillBlank && adto.BlankIndex.HasValue)
                        ans = q.QuestionAnswers.FirstOrDefault(a => a.Content != null && 
                              System.Text.RegularExpressions.Regex.IsMatch(a.Content, $@"placeholder\[{adto.BlankIndex}\](\{{|$)"));

                    if (ans == null) q.QuestionAnswers.Add(ans = new QuestionAnswer());

                    ans.Content = adto.Content;
                    ans.CorrectAnswer = adto.CorrectAnswer;
                    ans.IsCorrect = adto.IsCorrect;
                    ans.Point = adto.Point;

                    if (adto.InputTypeId.HasValue && adto.InputTypeId > 0)
                    {
                        var inp = ans.BlankInputs.FirstOrDefault();
                        if (inp == null) ans.BlankInputs.Add(inp = new BlankInput());
                        inp.InputTypeId = adto.InputTypeId.Value;
                    }
                    else ans.BlankInputs.Clear();
                }
            }
            return q;
        }

        private static int? GetBlankIndex(string? content) => 
            content != null && System.Text.RegularExpressions.Regex.Match(content, @"placeholder\[(\d+)\]") is { Success: true } m 
            ? int.Parse(m.Groups[1].Value) : null;

        private static (string? Stem, string? Frame) ParseContent(string? json)
        {
            if (string.IsNullOrEmpty(json)) return (null, null);
            try
            {
                var d = JsonSerializer.Deserialize<Dictionary<string, string>>(json, UnicodeJsonOptions);
                return (d?.GetValueOrDefault("stem") ?? d?.GetValueOrDefault("Stem"), 
                        d?.GetValueOrDefault("frame") ?? d?.GetValueOrDefault("Frame"));
            }
            catch
            {
                return (json, null); // Fallback for raw text
            }
        }

        private static QuestionSummaryDto MapToQuestionSummaryDto(Question question)
        {
            return new QuestionSummaryDto
            {
                QuestionId = question.QuestionId,
                ContentPreview = question.QuestionContent,
                QuestionType = question.QuestionType,
                Difficulty = question.Difficulty,
                DifficultyLabel = DifficultyLevel.GetLabel(question.Difficulty),
                SubjectCode = "",
                ChapterName = "",
                UpdatedAt = question.UpdatedAtUtc,
                Status = question.Status,
                QuestionPurpose = question.QuestionPurpose,
                QuestionPurposeLabel = Constants.QuestionPurpose.GetLabel(question.QuestionPurpose),
                AnswerCount = question.QuestionAnswers?.Count ?? 0
            };
        }

        private async Task HandleBlankGroupsAsync(Question q, QuestionDto item)
        {
            var answers = q.QuestionAnswers.ToList();
            var existingGroups = answers.Where(a => a.GroupAnswer != null).Select(a => a.GroupAnswer!).Distinct().ToList();
            
            if (item.BlankGroups?.Any() != true || item.QuestionType != QuestionType.FillBlank)
            {
                if (existingGroups.Any()) await _questionRepository.DeleteGroupAnswersAsync(existingGroups);
                answers.ForEach(a => { a.GroupAnswerId = null; a.GroupAnswer = null; });
                return;
            }

            var incomingIds = item.BlankGroups.Select(g => g.GroupAnswerId).Where(id => id.HasValue).ToHashSet();
            var toRemove = existingGroups.Where(g => !incomingIds.Contains(g.GroupAnswerId)).ToList();
            if (toRemove.Any()) await _questionRepository.DeleteGroupAnswersAsync(toRemove);

            answers.ForEach(a => { a.GroupAnswerId = null; a.GroupAnswer = null; });

            // Build list of groups in order (index = position in BlankGroups list)
            var groupsByIndex = new List<GroupAnswer>();

            foreach (var gDto in item.BlankGroups)
            {
                var group = gDto.GroupAnswerId.HasValue ? existingGroups.FirstOrDefault(g => g.GroupAnswerId == gDto.GroupAnswerId) : null;
                if (group == null) group = new GroupAnswer { Name = gDto.Name };
                else group.Name = gDto.Name;

                // Set DependsOnGroupId if it refers to an existing group
                if (gDto.DependsOnGroupId.HasValue)
                {
                    group.DependsOnGroupId = gDto.DependsOnGroupId.Value;
                }
                else
                {
                    group.DependsOnGroupId = null;
                }

                if (gDto.BlankIndices != null)
                {
                    foreach (var idx in gDto.BlankIndices)
                    {
                        var ans = answers.FirstOrDefault(a => a.Content != null && 
                                  System.Text.RegularExpressions.Regex.IsMatch(a.Content, $@"placeholder\[{idx}\](\{{|$)"));
                        if (ans != null) ans.GroupAnswer = group;
                    }
                }

                groupsByIndex.Add(group);
            }

            // Resolve index-based dependencies (DependsOnGroupIndex) for new groups
            for (int i = 0; i < item.BlankGroups.Count; i++)
            {
                var gDto = item.BlankGroups[i];
                if (gDto.DependsOnGroupIndex.HasValue && !gDto.DependsOnGroupId.HasValue)
                {
                    var depIdx = gDto.DependsOnGroupIndex.Value;
                    if (depIdx >= 0 && depIdx < groupsByIndex.Count && depIdx != i)
                    {
                        groupsByIndex[i].DependsOnGroup = groupsByIndex[depIdx];
                    }
                }
            }
        }
    }
}
