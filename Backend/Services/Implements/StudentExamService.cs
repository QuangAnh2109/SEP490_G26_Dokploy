using Backend.DTOs.StudentExam;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using System.Text.Json.Nodes;

namespace Backend.Services.Implements
{
    public class StudentExamService : IStudentExamService
    {
        private readonly IStudentExamRepository _studentExamRepository;

        public StudentExamService(IStudentExamRepository studentExamRepository)
        {
            _studentExamRepository = studentExamRepository;
        }

        public async Task<ExamPaperDto?> GetExamPaperAsync(int studentId, int examId, int paperId)
        {
            // Verify that the student is actually assigned to the class of the exam
            var canTake = await _studentExamRepository.CanStudentTakeExamAsync(studentId, examId);
            if (!canTake)
            {
                throw new UnauthorizedAccessException("Bạn không thuộc lớp được chỉ định để tham gia bài thi này.");
            }

            var paper = await _studentExamRepository.GetPaperWithQuestionsAsync(examId, paperId);
            if (paper == null || paper.Exam == null) return null;

            // Get active submission to use its ID as a random seed
            var activeSubmission = await _studentExamRepository.GetAnyActiveSubmissionAsync(studentId);
            int seed = activeSubmission?.SubmissionId ?? paper.PaperId;

            return new ExamPaperDto
            {
                ExamId = paper.Exam.ExamId,
                Title = paper.Exam.Title ?? string.Empty,
                Description = paper.Exam.Description,
                MaxAttempts = paper.Exam.MaxAttempts,
                Duration = paper.Exam.Duration,
                PaperId = paper.PaperId,
                Code = paper.Code,
                Questions = paper.PaperQuestions.Select(pq => 
                {
                    var dto = new QuestionDto
                    {
                        QuestionId = pq.Question.QuestionId,
                        ContentLatex = pq.Question.ContentLatex,
                        QuestionType = pq.Question.QuestionType,
                        Difficulty = pq.Question.Difficulty,
                    };

                    ProcessQuestionData(dto, pq.Question.Answer, pq.Question.ContentLatex, pq.Question.QuestionType, seed);
                    return dto;
                }).ToList()
            };
        }

        private void ProcessQuestionData(QuestionDto dto, string? rawAnswer, string contentLatex, string questionType, int seed)
        {
            if (string.IsNullOrWhiteSpace(rawAnswer)) return;

            try
            {
                if (questionType == "MultipleChoice")
                {
                    List<string> optionsArray = new List<string>();
                    var node = JsonNode.Parse(rawAnswer);

                    if (node is JsonArray jsonArray)
                    {
                        foreach (var item in jsonArray) optionsArray.Add(item?.ToString() ?? "");
                    }
                    else if (node is JsonObject jsonObj && jsonObj.ContainsKey("opts") && jsonObj["opts"] is JsonArray optsArray)
                    {
                        foreach (var item in optsArray) optionsArray.Add(item?.ToString() ?? "");
                    }

                    // Fallback regex if JSON is empty but format is A. B. C. D.
                    if (optionsArray.Count == 0 && !string.IsNullOrWhiteSpace(contentLatex))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(contentLatex, @"(.*?)(?:A\.|A\))(.*?)(?:B\.|B\))(.*?)(?:C\.|C\))(.*?)(?:D\.|D\))(.*)", System.Text.RegularExpressions.RegexOptions.Singleline);
                        if (match.Success && match.Groups.Count == 6)
                        {
                            dto.ContentLatex = match.Groups[1].Value.Trim();
                            optionsArray.Add(match.Groups[2].Value.Trim());
                            optionsArray.Add(match.Groups[3].Value.Trim());
                            optionsArray.Add(match.Groups[4].Value.Trim());
                            optionsArray.Add(match.Groups[5].Value.Trim());
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
                            dto.Options.Add(new QuestionOptionDto 
                            { 
                                Id = letters[originalIndex], // The real ID to map back to correct answer
                                Text = optionsArray[originalIndex] 
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
                    throw new InvalidOperationException($"Bạn đang có bài thi '{active.Paper?.Exam?.Title ?? "khác"}' chưa nộp. Vui lòng nộp bài trước khi bắt đầu bài thi mới.");
                }
            }

            // If PaperId is not provided, pick a random one for this exam
            int assignedPaperId;
            if (request.PaperId.HasValue && request.PaperId.Value > 0)
            {
                assignedPaperId = request.PaperId.Value;
            }
            else
            {
                var randomPaper = await _studentExamRepository.GetRandomPaperForExamAsync(request.ExamId);
                if (randomPaper == null)
                {
                    throw new InvalidOperationException("No papers found for this exam.");
                }
                assignedPaperId = randomPaper.PaperId;
            }

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
                QuestionIndex = request.QuestionIndex,
                ResponseText = request.ResponseText
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
                QuestionIndex = r.QuestionIndex,
                ResponseText = r.ResponseText
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
    }
}
