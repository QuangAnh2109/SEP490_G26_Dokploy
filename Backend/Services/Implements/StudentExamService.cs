using Backend.DTOs.StudentExam;
using Backend.Helpers;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Backend.Services.Implements
{
    public class StudentExamService : IStudentExamService
    {
        private readonly IStudentExamRepository _studentExamRepository;

        private readonly ILogger<StudentExamService> _logger;

        public StudentExamService(IStudentExamRepository studentExamRepository, ILogger<StudentExamService> logger)
        {
            _studentExamRepository = studentExamRepository;
            _logger = logger;
        }

        public async Task<ExamPaperDto?> GetExamPaperAsync(int studentId, int examId)
        {
            // Verify that the student is actually assigned to the class of the exam
            var canTake = await _studentExamRepository.CanStudentTakeExamAsync(studentId, examId);
            if (!canTake)
            {
                throw new UnauthorizedAccessException("Bạn không thuộc lớp được chỉ định để tham gia bài thi này.");
            }

            // Get active submission to find assigned paper
            var activeSubmission = await _studentExamRepository.GetAnyActiveSubmissionAsync(studentId);
            if (activeSubmission == null || activeSubmission.Paper == null || activeSubmission.Paper.ExamId != examId) 
            {
                throw new InvalidOperationException("Bạn chưa bắt đầu bài thi này hoặc bài thi đã kết thúc.");
            }

            int paperId = activeSubmission.PaperId;
            var paper = await _studentExamRepository.GetPaperWithQuestionsAsync(examId, paperId);
            if (paper == null || paper.Exam == null) return null;

            int seed = activeSubmission.SubmissionId;

            var questions = paper.Questions.Select(q => 
            {
                // Parse QuestionContent JSON: {"stem":"...", "frame":"..."}
                string stemContent = q.QuestionContent;
                string? frameContent = null;

                try
                {
                    var parsedContent = System.Text.Json.JsonSerializer.Deserialize<QuestionContentFormat>(
                        q.QuestionContent, 
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (parsedContent != null)
                    {
                        if (!string.IsNullOrEmpty(parsedContent.stem))
                            stemContent = parsedContent.stem;
                        if (!string.IsNullOrEmpty(parsedContent.frame))
                            frameContent = parsedContent.frame;
                    }
                }
                catch
                {
                    // Not valid JSON — treat as plain text content
                    stemContent = q.QuestionContent;
                    frameContent = null;
                }

                var dto = new QuestionDto
                {
                    QuestionId = q.QuestionId,
                    MainQuestionAnswerId = q.QuestionAnswers.FirstOrDefault()?.QuestionAnswerId ?? 0,
                    ContentLatex = stemContent,
                    Frame = frameContent,
                    // When frame exists, provide ordered QuestionAnswerIds (1:1 with placeholders)
                    FrameAnswerIds = frameContent != null
                        ? q.QuestionAnswers.OrderBy(qa => qa.QuestionAnswerId).Select(qa => qa.QuestionAnswerId).ToList()
                        : null,
                    FrameAllowedInputs = frameContent != null
                        ? q.QuestionAnswers.ToDictionary(
                            qa => qa.QuestionAnswerId,
                            qa => qa.BlankInputs.Select(bi => bi.InputType.Name).ToList())
                        : null,
                    QuestionType = q.QuestionType,
                    Difficulty = q.Difficulty,
                };

                // Build answer JSON from QuestionAnswers collection
                var answerJson = BuildAnswerJson(q.QuestionAnswers, q.QuestionType);
                ProcessQuestionData(dto, answerJson, stemContent, q.QuestionType, seed);
                return dto;
            }).ToList();

            if (paper.Exam.ShuffleQuestion)
            {
                var random = new Random(seed);
                questions = questions.OrderBy(q => random.Next()).ToList();
            }

            return new ExamPaperDto
            {
                ExamId = paper.Exam.ExamId,
                Title = paper.Exam.Title ?? string.Empty,
                Description = paper.Exam.Description,
                MaxAttempts = paper.Exam.MaxAttempts,
                Duration = paper.Exam.Duration,
                PaperId = paper.PaperId,
                Code = paper.Code,
                Questions = questions
            };
        }

        private string? BuildAnswerJson(ICollection<QuestionAnswer> questionAnswers, string questionType)
        {
            if (questionAnswers == null || !questionAnswers.Any()) return null;

            if (questionType == "MultipleChoice" || questionType == "SingleChoice")
            {
                var opts = questionAnswers.Select(qa => new { id = qa.QuestionAnswerId, content = qa.Content }).ToList();
                return System.Text.Json.JsonSerializer.Serialize(opts);
            }
            else if (questionType == "StepByStep")
            {
                var steps = questionAnswers.Select((qa, idx) => new { s = idx + 1, h = qa.Content, a = qa.CorrectAnswer }).ToList();
                return System.Text.Json.JsonSerializer.Serialize(steps);
            }
            else if (questionType == "FillInBlank")
            {
                var blanks = questionAnswers.Select(qa => qa.CorrectAnswer).ToList();
                return System.Text.Json.JsonSerializer.Serialize(blanks);
            }

            return questionAnswers.FirstOrDefault()?.CorrectAnswer;
        }

        private void ProcessQuestionData(QuestionDto dto, string? rawAnswer, string questionContent, string questionType, int seed)
        {
            if (string.IsNullOrWhiteSpace(rawAnswer)) return;

            try
            {
                if (questionType == "MultipleChoice" || questionType == "SingleChoice")
                {
                    List<(int id, string content)> optionsArray = new List<(int, string)>();
                    var node = JsonNode.Parse(rawAnswer);

                    if (node is JsonArray jsonArray)
                    {
                        foreach (var item in jsonArray)
                        {
                            if (item is JsonObject obj && obj.ContainsKey("id") && obj.ContainsKey("content"))
                            {
                                optionsArray.Add(((int)(obj["id"]?.GetValue<int>() ?? 0), obj["content"]?.ToString() ?? ""));
                            }
                            else
                            {
                                optionsArray.Add((0, item?.ToString() ?? ""));
                            }
                        }
                    }
                    else if (node is JsonObject jsonObj && jsonObj.ContainsKey("opts") && jsonObj["opts"] is JsonArray optsArray)
                    {
                        foreach (var item in optsArray)
                        {
                            optionsArray.Add((0, item?.ToString() ?? ""));
                        }
                    }

                    // Fallback regex if JSON is empty but format is A. B. C. D.
                    if (optionsArray.Count == 0 && !string.IsNullOrWhiteSpace(questionContent))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(questionContent, @"(.*?)(?:A\.|A\))(.*?)(?:B\.|B\))(.*?)(?:C\.|C\))(.*?)(?:D\.|D\))(.*)", System.Text.RegularExpressions.RegexOptions.Singleline);
                        if (match.Success && match.Groups.Count == 6)
                        {
                            dto.ContentLatex = match.Groups[1].Value.Trim();
                            optionsArray.Add((0, match.Groups[2].Value.Trim()));
                            optionsArray.Add((0, match.Groups[3].Value.Trim()));
                            optionsArray.Add((0, match.Groups[4].Value.Trim()));
                            optionsArray.Add((0, match.Groups[5].Value.Trim()));
                        }
                    }

                    if (optionsArray.Count > 0)
                    {
                        // Shuffle options using the seed
                        var random = new Random(seed + dto.QuestionId);
                        var shuffledIndices = Enumerable.Range(0, optionsArray.Count).OrderBy(x => random.Next()).ToList();
                        
                        var letters = new[] { "A", "B", "C", "D", "E", "F" };
                        dto.Options = new List<QuestionOptionDto>();
                        
                        for (int i = 0; i < shuffledIndices.Count && i < letters.Length; i++)
                        {
                            int originalIndex = shuffledIndices[i];
                            int qaId = optionsArray[originalIndex].id;
                            string optIdString = qaId > 0 ? qaId.ToString() : letters[originalIndex];
                            
                            dto.Options.Add(new QuestionOptionDto 
                            { 
                                Id = optIdString, // The real QuestionAnswerId or fallback letter
                                Text = optionsArray[originalIndex].content 
                            });
                        }
                    }
                }
                else if (questionType == "StepByStep")
                {
                    var node = JsonNode.Parse(rawAnswer);
                    if (node is JsonArray jsonArray)
                    {
                        dto.Steps = new List<QuestionStepDto>();
                        foreach (var item in jsonArray)
                        {
                            if (item is JsonObject stepObj)
                            {
                                int s = (int?)stepObj["s"] ?? (int?)stepObj["step"] ?? (int?)stepObj["Step"] ?? 0;
                                string? h = (string?)stepObj["h"] ?? (string?)stepObj["hint"] ?? (string?)stepObj["Hint"];
                                
                                if (s > 0)
                                {
                                    dto.Steps.Add(new QuestionStepDto { Step = s, Hint = h });
                                }
                            }
                        }
                    }
                }
                else if (questionType == "FillInBlank")
                {
                    var node = JsonNode.Parse(rawAnswer);
                    if (node is JsonArray jsonArray)
                    {
                        var dummyArray = new JsonArray();
                        foreach (var _ in jsonArray)
                        {
                            dummyArray.Add("");
                        }
                        dto.Answer = dummyArray.ToJsonString();
                    }
                }
            }
            catch
            {
                // If parsing fails, do nothing. Dto will just have empty options/steps.
            }
        }

        private string SanitizeAnswer(string? answer, string? questionType)
        {
            if (string.IsNullOrWhiteSpace(answer)) return string.Empty;

            try
            {
                if (questionType == "MultipleChoice")
                {
                    var node = JsonNode.Parse(answer);
                    if (node is JsonObject jsonObj)
                    {
                        jsonObj.Remove("correct");
                        jsonObj.Remove("Correct");
                        return jsonObj.ToJsonString();
                    }
                    return answer;
                }
                else if (questionType == "StepByStep")
                {
                    var node = JsonNode.Parse(answer);
                    if (node is JsonArray jsonArray)
                    {
                        foreach (var item in jsonArray)
                        {
                            if (item is JsonObject stepObj)
                            {
                                stepObj.Remove("a");
                                stepObj.Remove("answer");
                                stepObj.Remove("Answer");
                            }
                        }
                        return jsonArray.ToJsonString();
                    }
                    return answer;
                }
                
                // For other types (e.g. ShortAnswer), the answer is just the correct text. We don't send it.
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<Submission> StartExamAsync(int studentId, StartSubmissionRequest request)
        {
            // Verify that the student is actually assigned to the class of the exam
            var canTake = await _studentExamRepository.CanStudentTakeExamAsync(studentId, request.ExamId);
            if (!canTake)
            {
                throw new UnauthorizedAccessException("Bạn không thuộc lớp được chỉ định để tham gia bài thi này.");
            }

            // Check if student already has ANY active submission
            var active = await _studentExamRepository.GetAnyActiveSubmissionAsync(studentId);
            if (active != null)
            {
                if (active.Paper != null && active.Paper.ExamId == request.ExamId)
                {
                    return active; // Return existing submission to continue for THIS exam
                }
                else
                {
                    throw new InvalidOperationException($"Bạn đang có bài thi khác chưa nộp. Vui lòng hoàn thành hoặc nộp bài đó trước khi bắt đầu bài thi mới.|ACTIVE_EXAM_ID:{active.Paper?.ExamId}");
                }
            }

            // Pick a random paper for this exam
            int assignedPaperId;
            var randomPaper = await _studentExamRepository.GetRandomPaperForExamAsync(request.ExamId);
            if (randomPaper == null)
            {
                throw new InvalidOperationException("No papers found for this exam.");
            }
            assignedPaperId = randomPaper.PaperId;

            // Check if student has reached MaxAttempts for this exam
            var paper = await _studentExamRepository.GetPaperWithExamAsync(assignedPaperId);
            if (paper != null && paper.Exam != null)
            {
                var attemptCount = await _studentExamRepository.GetExamSubmissionCountAsync(studentId, paper.ExamId);
                if (paper.Exam.MaxAttempts > 0 && attemptCount >= paper.Exam.MaxAttempts)
                {
                    throw new InvalidOperationException("Maximum attempts reached for this exam.");
                }
            }

            var submission = new Submission
            {
                StudentId = studentId,
                PaperId = assignedPaperId,
                Status = 1, 
                CreatedAtUtc = DateTime.UtcNow,
                Paper = paper!
            };

            return await _studentExamRepository.CreateSubmissionAsync(submission);
        }

        public async Task SaveAnswerAsync(int studentId, int submissionId, SubmitAnswerRequest request)
        {
            var submission = await _studentExamRepository.GetSubmissionByIdAsync(submissionId);
            if (submission == null) throw new InvalidOperationException("Submission not found.");
            
            if (submission.StudentId != studentId) 
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa bài làm này.");

            if (submission.Status != 1)
                throw new InvalidOperationException("Bài thi không ở trạng thái đang làm.");

            if (submission.Paper?.Exam != null)
            {
                var now = DateTime.UtcNow;
                var durationSeconds = submission.Paper.Exam.Duration * 60;
                var elapsedSeconds = (now - submission.CreatedAtUtc).TotalSeconds;
                
                if (elapsedSeconds > durationSeconds || (submission.Paper.Exam.CloseAt.HasValue && now >= submission.Paper.Exam.CloseAt.Value))
                {
                    await _studentExamRepository.CompleteSubmissionAsync(submissionId);
                    throw new InvalidOperationException("Đã hết thời gian làm bài hoặc kỳ thi đã đóng, hệ thống đã nộp bài tự động.");
                }
            }

            var answer = new StudentAnswer
            {
                SubmissionId = submissionId,
                QuestionAnswerId = request.QuestionAnswerId,
                Response = request.Response
            };

            await _studentExamRepository.AddOrUpdateBulkStudentAnswersAsync(new[] { answer });
        }

        public async Task SaveBulkAnswersAsync(int studentId, int submissionId, IEnumerable<SubmitAnswerRequest> requests)
        {
            var submission = await _studentExamRepository.GetSubmissionByIdAsync(submissionId);
            if (submission == null) throw new InvalidOperationException("Submission not found.");
            
            if (submission.StudentId != studentId) 
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa bài làm này.");

            if (submission.Status != 1)
                throw new InvalidOperationException("Bài thi không ở trạng thái đang làm.");

            if (submission.Paper?.Exam != null)
            {
                var now = DateTime.UtcNow;
                var durationSeconds = submission.Paper.Exam.Duration * 60;
                var elapsedSeconds = (now - submission.CreatedAtUtc).TotalSeconds;
                
                if (elapsedSeconds > durationSeconds || (submission.Paper.Exam.CloseAt.HasValue && now >= submission.Paper.Exam.CloseAt.Value))
                {
                    await _studentExamRepository.CompleteSubmissionAsync(submissionId);
                    throw new InvalidOperationException("Đã hết thời gian làm bài hoặc kỳ thi đã đóng, hệ thống đã nộp bài tự động.");
                }
            }

            var answers = requests.Select(r => new StudentAnswer
            {
                SubmissionId = submissionId,
                QuestionAnswerId = r.QuestionAnswerId,
                Response = r.Response
            });

            await _studentExamRepository.AddOrUpdateBulkStudentAnswersAsync(answers);
        }

        public async Task SubmitExamAsync(int studentId, int submissionId)
        {
            var submission = await _studentExamRepository.GetSubmissionByIdAsync(submissionId);
            if (submission == null) throw new InvalidOperationException("Submission not found.");
            
            if (submission.StudentId != studentId) 
                throw new UnauthorizedAccessException("Bạn không có quyền nộp bài làm này.");

            await _studentExamRepository.CompleteSubmissionAsync(submissionId);
        }

        public async Task ForceSubmitOverdueSubmissionsAsync(int examId)
        {
            await _studentExamRepository.ForceSubmitOverdueExamsAsync(examId);
        }

        public async Task<ExamPreviewDto?> GetExamPreviewAsync(int studentId, int examId)
        {
            var canTake = await _studentExamRepository.CanStudentTakeExamAsync(studentId, examId);
            if (!canTake)
            {
                throw new UnauthorizedAccessException("Bạn không thuộc lớp được chỉ định để xem bài thi này.");
            }

            var data = await _studentExamRepository.GetExamPreviewAsync(examId);
            if (data == null) return null;

            var statusLabel = data.Status switch
            {
                1 => "public",
                2 => "private",
                3 => "closed",
                _ => "unknown"
            };

            var matrixRows = data.BlueprintChapters
                .GroupBy(x => x.ChapterName)
                .Select(g => new BlueprintRowDto
                {
                    ChapterName = g.Key,
                    Recognize = g.Where(x => x.Difficulty == 1).Sum(x => x.TotalOfQuestions),
                    Understand = g.Where(x => x.Difficulty == 2).Sum(x => x.TotalOfQuestions),
                    Apply = g.Where(x => x.Difficulty == 3).Sum(x => x.TotalOfQuestions),
                    AdvancedApply = g.Where(x => x.Difficulty == 4).Sum(x => x.TotalOfQuestions),
                    Total = g.Sum(x => x.TotalOfQuestions)
                })
                .ToList();

            return new ExamPreviewDto
            {
                ExamId = data.ExamId,
                SubjectCode = data.SubjectCode,
                Title = data.Title,
                TotalQuestions = data.TotalQuestions,
                Duration = data.Duration,
                OpenAt = data.OpenAt,
                CloseAt = data.CloseAt,
                Status = statusLabel,
                TeacherName = data.TeacherName,
                UpdatedAtUtc = data.UpdatedAtUtc,
                Description = data.Description,
                BlueprintMatrix = matrixRows
            };
        }

        public async Task<TakeExamDto?> TakeExamInClass(int examId, int studentId)
        {
            // 1. Kiểm tra exam tồn tại, student thuộc lớp, thời gian hợp lệ
            var examInfo = await _studentExamRepository.GetExamInfoForStudentAsync(examId, studentId);
            if (examInfo == null)
            {
                return null;
            }

            // 2. Kiểm tra submission hiện tại của học sinh
            Submission? activeSubmission = null;
            var anyActiveSubmission = await _studentExamRepository.GetAnyActiveSubmissionAsync(studentId);

            if (anyActiveSubmission != null)
            {
                if (anyActiveSubmission.Paper != null && anyActiveSubmission.Paper.ExamId == examId)
                {
                    // Đang làm bài cùng examId → cho phép tiếp tục
                    activeSubmission = anyActiveSubmission;
                }
                else
                {
                    // Đang làm bài khác examId → từ chối
                    throw new InvalidOperationException(
                        "Bạn đang có bài thi khác chưa nộp. Vui lòng hoàn thành hoặc nộp bài đó trước khi bắt đầu bài thi mới.");
                }
            }

            // 3. Nếu không có submission active → tạo mới
            if (activeSubmission == null)
            {
                // Kiểm tra MaxAttempts  
                if (examInfo.MaxAttempts > 0 && examInfo.StudentAttempts >= examInfo.MaxAttempts)
                {
                    throw new InvalidOperationException("Bạn đã hết lượt làm bài cho bài thi này.");
                }

                // Random chọn PaperId
                if (examInfo.PaperIds == null || examInfo.PaperIds.Count == 0)
                {
                    throw new InvalidOperationException("Không tìm thấy đề thi nào cho bài kiểm tra này.");
                }

                var random = new Random();
                int randomIndex = random.Next(examInfo.PaperIds.Count);
                int selectedPaperId = examInfo.PaperIds[randomIndex];

                var newSubmission = new Submission
                {
                    StudentId = studentId,
                    PaperId = selectedPaperId,
                    Status = 1, // Chưa nộp
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                activeSubmission = await _studentExamRepository.CreateSubmissionAsync(newSubmission);
            }

            // 4. Lấy paper với questions, answers, input types
            var paper = await _studentExamRepository.GetPaperWithQuestionsAsync(examId, activeSubmission.PaperId);
            if (paper == null || paper.Exam == null)
            {
                return null;
            }

            // 5. Map sang TakeExamQuestionDto
            var questions = paper.Questions.Select(q =>
            {
                // Parse QuestionContent JSON: {"stem":"...", "frame":"..."}
                string stemContent;
                string? frameContent = null;

                try
                {
                    string sanitizedContent = q.QuestionContent;

                    var parsed = JsonSerializer.Deserialize<QuestionContentFormat>(sanitizedContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    stemContent = parsed.stem;
                    frameContent = parsed?.frame;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("JSON Error tại QuestionId {Id}: {Msg}. Dữ liệu thô: {Raw}",
                        q.QuestionId, ex.Message, q.QuestionContent);

                    stemContent = q.QuestionContent;
                }

                // Map answers
                var answers = q.QuestionAnswers.Select(qa => new TakeExamAnswerDto
                {
                    QuestionAnswerId = SecureIdHelper.EncryptId(qa.QuestionAnswerId),
                    Content = qa.Content,
                    GroupAnswerId = qa.GroupAnswerId.HasValue
                        ? SecureIdHelper.EncryptId(qa.GroupAnswerId.Value)
                        : null,
                    InputTypes = qa.BlankInputs.Select(bi => new TakeExamInputTypeDto
                    {
                        InputTypeId = SecureIdHelper.EncryptId(bi.InputTypeId),
                        Name = bi.InputType.Name,
                        GroupType = bi.InputType.GroupType
                    }).ToList()
                }).ToList();

                return new TakeExamQuestionDto
                {
                    QuestionId = SecureIdHelper.EncryptId(q.QuestionId),
                    QuestionType = q.QuestionType,
                    Stem = stemContent,
                    Frame = frameContent,
                    Difficulty = q.Difficulty,
                    Answers = answers
                };
            }).ToList();

            // 6. Shuffle questions nếu cần
            if (paper.Exam.ShuffleQuestion)
            {
                questions.Shuffle();
            }

            // 7. Trả về TakeExamDto
            return new TakeExamDto
            {
                ExamId = SecureIdHelper.EncryptId(paper.Exam.ExamId),
                SubmissionId = SecureIdHelper.EncryptId(activeSubmission.SubmissionId),
                Title = paper.Exam.Title ?? string.Empty,
                Duration = paper.Exam.Duration,
                Code = paper.Code,
                Questions = questions
            };
        }
    }
}
