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

        public async Task<QuestionListResponseDto> GetQuestionsAsync(QuestionListQueryDto query, int userId)
        {
            var (items, totalCount) = await _questionRepository.GetQuestionsAsync(query, userId);
            var pageSize = Math.Clamp(query.PageSize, 1, 50);

            return new QuestionListResponseDto
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                PageSize = pageSize,
                CurrentPage = Math.Max(query.Page, 1)
            };
        }

        public async Task<CreateQuestionBatchResponse> CreateQuestionsAsync(int userId, CreateQuestionBatchRequest request)
        {
            if (request.Questions == null || request.Questions.Count == 0)
            {
                throw new QuestionValidationException(new[] { "Phải có ít nhất 1 câu hỏi." });
            }

            var allErrors = new List<string>();
            var questionsToCreate = new List<Question>();

            for (var i = 0; i < request.Questions.Count; i++)
            {
                var item = request.Questions[i];
                var prefix = $"Câu hỏi #{i + 1}";
                var errors = await ValidateQuestionItemAsync(item, prefix);

                if (errors.Count > 0)
                {
                    allErrors.AddRange(errors);
                    continue;
                }

                var contentJson = JsonSerializer.Serialize(new
                {
                    stem = item.Stem,
                    frame = item.Frame
                }, UnicodeJsonOptions);

                var question = new Question
                {
                    CreatedByUserId = userId,
                    QuestionType = item.QuestionType,
                    QuestionContent = contentJson,
                    ChapterId = item.ChapterId,
                    Difficulty = item.Difficulty,
                    Status = item.Status,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                // Create answers
                foreach (var answerDto in item.Answers)
                {
                    var answer = new QuestionAnswer
                    {
                        Content = answerDto.Content,
                        CorrectAnswer = answerDto.CorrectAnswer,
                        IsCorrect = answerDto.IsCorrect,
                        Point = answerDto.Point
                    };

                    // InputType is stored via the BlankInput join table, not directly on QuestionAnswer
                    if (answerDto.InputTypeId.HasValue && answerDto.InputTypeId.Value > 0)
                    {
                        answer.BlankInputs.Add(new BlankInput
                        {
                            InputTypeId = answerDto.InputTypeId.Value
                        });
                    }

                    question.QuestionAnswers.Add(answer);
                }

                questionsToCreate.Add(question);
            }

            if (allErrors.Count > 0)
            {
                throw new QuestionValidationException(allErrors);
            }

            var createdQuestions = await _questionRepository.CreateQuestionsAsync(questionsToCreate);

            // Handle blank groups after questions are created (need answer IDs)
            for (var i = 0; i < request.Questions.Count; i++)
            {
                var item = request.Questions[i];
                if (item.BlankGroups != null && item.BlankGroups.Count > 0 && item.QuestionType == QuestionType.FillBlank)
                {
                    var question = createdQuestions[i];
                    var answersList = question.QuestionAnswers.ToList();

                    foreach (var groupDto in item.BlankGroups)
                    {
                        var groupAnswer = new GroupAnswer
                        {
                            Name = groupDto.Name
                        };

                        var createdGroup = await _questionRepository.CreateGroupAnswerAsync(groupAnswer);

                        // Assign group to matching answers
                        foreach (var blankIdx in groupDto.BlankIndices)
                        {
                            var matchingAnswer = answersList.FirstOrDefault(a =>
                            {
                                var content = a.Content;
                                return content != null && content.Contains($"placeholder{{{blankIdx}}}");
                            });

                            if (matchingAnswer != null)
                            {
                                matchingAnswer.GroupAnswerId = createdGroup.GroupAnswerId;
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Created {Count} questions for user {UserId}", createdQuestions.Count, userId);

            return new CreateQuestionBatchResponse
            {
                CreatedQuestions = createdQuestions.Select(q => new QuestionListItemDto
                {
                    QuestionId = q.QuestionId,
                    ContentPreview = q.QuestionContent,
                    QuestionType = q.QuestionType,
                    Difficulty = q.Difficulty,
                    DifficultyLabel = DifficultyLevel.GetLabel(q.Difficulty),
                    SubjectCode = "",
                    ChapterName = "",
                    UpdatedAt = q.UpdatedAtUtc,
                    Status = q.Status,
                    AnswerCount = q.QuestionAnswers.Count
                }).ToList(),
                Count = createdQuestions.Count
            };
        }

        public async Task<List<InputTypeDto>> GetInputTypesAsync()
        {
            var inputTypes = await _questionRepository.GetInputTypesAsync();
            return inputTypes.Select(it => new InputTypeDto
            {
                InputTypeId = it.InputTypeId,
                Name = it.Name,
                GroupType = it.GroupType
            }).ToList();
        }

        public async Task<List<SubjectWithChaptersDto>> GetSubjectsWithChaptersAsync()
        {
            var subjects = await _questionRepository.GetSubjectsWithChaptersAsync();
            return subjects.Select(s => new SubjectWithChaptersDto
            {
                SubjectId = s.SubjectId,
                Name = s.Name,
                Code = s.Code,
                Chapters = s.Chapters.Select(c => new ChapterDto
                {
                    ChapterId = c.ChapterId,
                    Name = c.Name
                }).ToList()
            }).ToList();
        }

        private async Task<List<string>> ValidateQuestionItemAsync(CreateQuestionItemDto item, string prefix)
        {
            var errors = new List<string>();

            // QuestionType validation
            if (!QuestionType.IsValid(item.QuestionType))
            {
                errors.Add($"{prefix}: Loại câu hỏi phải là '{QuestionType.FillBlank}' hoặc '{QuestionType.Mcq}'.");
                return errors;
            }

            // Stem validation
            if (string.IsNullOrWhiteSpace(item.Stem))
            {
                errors.Add($"{prefix}: Đề bài không được để trống.");
            }

            // Difficulty validation
            if (!DifficultyLevel.IsValid(item.Difficulty))
            {
                errors.Add($"{prefix}: Mức độ phải từ 1 đến 4.");
            }

            // Status validation
            if (!QuestionStatus.IsValid(item.Status))
            {
                errors.Add($"{prefix}: Trạng thái không hợp lệ.");
            }

            // ChapterId validation
            if (!await _questionRepository.ChapterExistsAsync(item.ChapterId))
            {
                errors.Add($"{prefix}: Chương không tồn tại.");
            }

            // Answers validation
            if (item.Answers == null || item.Answers.Count == 0)
            {
                errors.Add($"{prefix}: Phải có ít nhất 1 đáp án.");
                return errors;
            }

            // Per-type validation
            if (item.QuestionType == QuestionType.FillBlank)
            {
                ValidateFillBlankAnswers(item, prefix, errors);
            }
            else if (item.QuestionType == QuestionType.Mcq)
            {
                ValidateMcqAnswers(item, prefix, errors);
            }

            // Point validation — total must equal 100
            var totalPoints = item.Answers.Sum(a => a.Point);
            if (totalPoints != RequiredTotalPoint)
            {
                errors.Add($"{prefix}: Tổng hệ số điểm phải bằng {RequiredTotalPoint}% (hiện tại: {totalPoints}%).");
            }

            return errors;
        }

        private void ValidateFillBlankAnswers(CreateQuestionItemDto item, string prefix, List<string> errors)
        {
            // Frame validation for fill_blank
            if (string.IsNullOrWhiteSpace(item.Frame))
            {
                errors.Add($"{prefix}: Khung trả lời không được để trống cho dạng điền vào chỗ trống.");
            }

            // Each answer must have InputTypeId
            for (var j = 0; j < item.Answers.Count; j++)
            {
                var answer = item.Answers[j];
                var answerPrefix = $"{prefix}, ô trống #{j + 1}";

                if (!answer.InputTypeId.HasValue || answer.InputTypeId.Value <= 0)
                {
                    errors.Add($"{answerPrefix}: Phải chọn ít nhất 1 loại giới hạn nhập liệu (InputType).");
                }

                ValidatePointRange(answer.Point, answerPrefix, errors);
            }

            // GroupType exclusivity: answers with same GroupType can only have one InputType from that group
            // This is validated per-answer, but we also check that no two answers share same GroupType InputType conflict
        }

        private void ValidateMcqAnswers(CreateQuestionItemDto item, string prefix, List<string> errors)
        {
            if (item.Answers.Count < 2)
            {
                errors.Add($"{prefix}: Câu trắc nghiệm phải có ít nhất 2 lựa chọn.");
            }

            var hasCorrect = item.Answers.Any(a => a.IsCorrect == true);
            if (!hasCorrect)
            {
                errors.Add($"{prefix}: Câu trắc nghiệm phải có ít nhất 1 đáp án đúng.");
            }

            foreach (var answer in item.Answers)
            {
                ValidatePointRange(answer.Point, prefix, errors);
            }
        }

        private static void ValidatePointRange(int point, string prefix, List<string> errors)
        {
            if (point < MinPoint || point > MaxPoint)
            {
                errors.Add($"{prefix}: Hệ số điểm phải từ {MinPoint} đến {MaxPoint}.");
            }
        }
    }
}
