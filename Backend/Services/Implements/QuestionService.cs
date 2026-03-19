using Backend.Constants;
using Backend.DTOs.Question;
using Backend.Exceptions;
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

        private const int MinPoint = 0;
        private const int MaxPoint = 100;
        private const int RequiredTotalPoint = 100;

        private static readonly JsonSerializerOptions UnicodeJsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public QuestionService(IQuestionRepository questionRepository, ILogger<QuestionService> logger)
        {
            _questionRepository = questionRepository;
            _logger = logger;
        }

        public async Task<QuestionListResultDto> GetQuestionsAsync(QuestionListQueryDto query, int userId)
        {
            var (items, totalCount) = await _questionRepository.GetQuestionsAsync(query, userId);
            var pageSize = Math.Clamp(query.PageSize, 1, 50);

            return new QuestionListResultDto
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                PageSize = pageSize,
                CurrentPage = Math.Max(query.Page, 1)
            };
        }

        public async Task<List<QuestionSummaryDto>> CreateQuestionsAsync(int userId, List<QuestionDto> request)
        {
            if (!(request?.Any() ?? false)) throw new QuestionValidationException(new[] { "Phải có ít nhất 1 câu hỏi." });

            var errors = new List<string>();
            for (int i = 0; i < request.Count; i++)
                errors.AddRange(await ValidateQuestionItemAsync(request[i], $"Câu hỏi #{i + 1}"));

            if (errors.Any()) throw new QuestionValidationException(errors);

            var created = await _questionRepository.CreateQuestionsAsync(request.Select(q => MapToQuestionEntity(q, userId)).ToList());
            for (int i = 0; i < request.Count; i++)
                await HandleBlankGroupsAsync(created[i], request[i]);

            await _questionRepository.SaveChangesAsync();
            return created.Select(MapToQuestionSummaryDto).ToList();
        }

        public async Task<QuestionSummaryDto> UpdateQuestionAsync(int questionId, int userId, QuestionDto request)
        {
            var existing = await _questionRepository.GetQuestionWithAnswersAsync(questionId);
            if (existing == null || existing.CreatedByUserId != userId) throw new KeyNotFoundException("Không tìm thấy câu hỏi hoặc bạn không có quyền sửa.");
            if (existing.Status == QuestionStatus.Archive) throw new QuestionValidationException(new[] { "Không thể sửa câu hỏi đã lưu trữ (Archive)." });

            var errors = await ValidateQuestionItemAsync(request, "Câu hỏi");
            if (errors.Any()) throw new QuestionValidationException(errors);

            var isUsed = await _questionRepository.IsQuestionUsedAsync(questionId);
            Question result;

            if (existing.Status == QuestionStatus.Inprogess || isUsed)
            {
                existing.Status = QuestionStatus.Archive;
                if (request.Status == QuestionStatus.Inprogess) request.Status = QuestionStatus.Active;
                result = MapToQuestionEntity(request, userId);
                await _questionRepository.CreateQuestionsAsync(new List<Question> { result });
                _logger.LogInformation("Cloned question {OldId} into {NewId} (Used={IsUsed}).", questionId, result.QuestionId, isUsed);
            }
            else
            {
                var incomingIds = request.Answers.Select(a => a.AnswerId).Where(id => id.HasValue).ToHashSet();
                var toRemove = existing.QuestionAnswers.Where(a => !incomingIds.Contains(a.QuestionAnswerId)).ToList();
                if (toRemove.Any()) await _questionRepository.DeleteQuestionAnswersAsync(toRemove);
                result = MapToQuestionEntity(request, userId, existing);
            }

            await HandleBlankGroupsAsync(result, request);
            await _questionRepository.SaveChangesAsync();
            return MapToQuestionSummaryDto(result);
        }

        public async Task<QuestionDto> GetQuestionByIdAsync(int questionId, int userId)
        {
            var q = await _questionRepository.GetQuestionWithAnswersAsync(questionId);
            if (q == null || q.CreatedByUserId != userId) throw new KeyNotFoundException("Không tìm thấy câu hỏi hoặc bạn không có quyền xem.");

            var (stem, frame) = ParseContent(q.QuestionContent);
            var dto = new QuestionDto { QuestionType = q.QuestionType, ChapterId = q.ChapterId, Difficulty = q.Difficulty, Status = q.Status, Stem = stem ?? string.Empty, Frame = frame };

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
            return dto;
        }

        public async Task<int> UpdateQuestionStatusAsync(List<int> questionIds, int userId, string status)
        {
            if (!QuestionStatus.IsValid(status))
            {
                throw new QuestionValidationException(new[] { "Trạng thái không hợp lệ." });
            }

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

            return updatedCount;
        }

        private Question MapToQuestionEntity(QuestionDto item, int userId, Question? existing = null)
        {
            var q = existing ?? new Question { CreatedByUserId = userId };
            q.QuestionType = item.QuestionType;
            q.ChapterId = item.ChapterId;
            q.Difficulty = item.Difficulty;
            q.Status = item.Status;
            q.UpdatedAtUtc = DateTime.UtcNow;
            q.QuestionContent = JsonSerializer.Serialize(new { stem = item.Stem, frame = item.Frame }, UnicodeJsonOptions);

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

                if (adto.InputTypeId > 0)
                {
                    var inp = ans.BlankInputs.FirstOrDefault();
                    if (inp == null) ans.BlankInputs.Add(inp = new BlankInput());
                    inp.InputTypeId = adto.InputTypeId.Value;
                }
                else ans.BlankInputs.Clear();
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

            foreach (var gDto in item.BlankGroups)
            {
                var group = gDto.GroupAnswerId.HasValue ? existingGroups.FirstOrDefault(g => g.GroupAnswerId == gDto.GroupAnswerId) : null;
                if (group == null) group = new GroupAnswer { Name = gDto.Name, DependsOnGroupId = gDto.DependsOnGroupId };
                else { group.Name = gDto.Name; group.DependsOnGroupId = gDto.DependsOnGroupId; }

                foreach (var idx in gDto.BlankIndices)
                {
                    var ans = answers.FirstOrDefault(a => a.Content != null && 
                              System.Text.RegularExpressions.Regex.IsMatch(a.Content, $@"placeholder\[{idx}\](\{{|$)"));
                    if (ans != null) ans.GroupAnswer = group;
                }
            }
        }

        public async Task<QuestionMetadataDto> GetQuestionMetadataAsync()
        {
            var inputTypes = await _questionRepository.GetInputTypesAsync();
            var subjects = await _questionRepository.GetSubjectsWithChaptersAsync();

            return new QuestionMetadataDto
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
            };
        }

        private async Task<List<string>> ValidateQuestionItemAsync(QuestionDto item, string prefix)
        {
            var errs = new List<string>();
            if (!QuestionType.IsValid(item.QuestionType)) return new() { $"{prefix}: Loại câu hỏi không hợp lệ." };
            if (string.IsNullOrWhiteSpace(item.Stem)) errs.Add($"{prefix}: Đề bài không được để trống.");
            if (!DifficultyLevel.IsValid(item.Difficulty)) errs.Add($"{prefix}: Mức độ phải từ 1 đến 4.");
            if (!QuestionStatus.IsValid(item.Status)) errs.Add($"{prefix}: Trạng thái không hợp lệ.");
            if (!await _questionRepository.ChapterExistsAsync(item.ChapterId)) errs.Add($"{prefix}: Chương không tồn tại.");
            if (!(item.Answers?.Any() ?? false)) return new() { $"{prefix}: Phải có ít nhất 1 đáp án." };

            if (item.QuestionType == QuestionType.FillBlank)
            {
                if (string.IsNullOrWhiteSpace(item.Frame)) errs.Add($"{prefix}: Khung trả lời không được để trống.");
                for (int i = 0; i < item.Answers.Count; i++)
                {
                    var a = item.Answers[i];
                    if (a.InputTypeId <= 0) errs.Add($"{prefix}, ô trống #{i + 1}: Thiếu loại giới hạn nhập liệu.");
                    ValidatePointRange(a.Point, $"{prefix}, ô trống #{i + 1}", errs);
                }
            }
            else
            {
                if (item.Answers.Count < 2) errs.Add($"{prefix}: Câu trắc nghiệm phải có ít nhất 2 lựa chọn.");
                if (!item.Answers.Any(a => a.IsCorrect == true)) errs.Add($"{prefix}: Phải có ít nhất 1 đáp án đúng.");
                item.Answers.ForEach(a => ValidatePointRange(a.Point, prefix, errs));
            }

            if (item.Answers.Sum(a => a.Point) != RequiredTotalPoint) errs.Add($"{prefix}: Tổng điểm phải bằng {RequiredTotalPoint}%.");
            return errs;
        }

        private static void ValidatePointRange(int pt, string pfx, List<string> errs)
        {
            if (pt < MinPoint || pt > MaxPoint) errs.Add($"{pfx}: Điểm phải từ {MinPoint} đến {MaxPoint}.");
        }
    }
}
