using System.Text.Json;
using System.Text.Json.Nodes;

using Backend.Constants;
using Backend.DTOs.StudentExam;
using Backend.Helpers;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Backend.Services.Implements
{
    public class StudentExamService : IStudentExamService
    {
        private readonly IStudentExamRepository _studentExamRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<StudentExamService> _logger;

        public StudentExamService(IStudentExamRepository studentExamRepository, IMemoryCache cache, ILogger<StudentExamService> logger)
        {
            _studentExamRepository = studentExamRepository;
            _cache = cache;
            _logger = logger;
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
                    Status = SubmissionStatus.InProgress,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                activeSubmission = await _studentExamRepository.CreateSubmissionAsync(newSubmission);
            }

            // 4. Lấy nội dung câu hỏi (Cache theo PaperId)
            string cacheKey = $"ExamContent_Paper_{activeSubmission.PaperId}";
            if (!_cache.TryGetValue(cacheKey, out List<TakeExamQuestionDto>? questions) || questions == null)
            {
                var paper = await _studentExamRepository.GetPaperWithQuestionsAsync(examId, activeSubmission.PaperId);
                if (paper == null) return null;

                // Map sang TakeExamQuestionDto
                questions = paper.Questions.Select(q =>
                {
                    var answers = q.QuestionAnswers.Select(qa => new TakeExamAnswerDto
                    {
                        QuestionAnswerId = qa.QuestionAnswerId.ToString(),
                        Content = qa.Content,
                        GroupAnswerId = qa.GroupAnswerId?.ToString(),
                        InputTypes = qa.BlankInputs.Select(bi => new TakeExamInputTypeDto
                        {
                            InputTypeId = bi.InputTypeId.ToString(),
                            Name = bi.InputType.Name,
                            GroupType = bi.InputType.GroupType
                        }).ToList()
                    }).ToList();

                    return new TakeExamQuestionDto
                    {
                        QuestionId = q.QuestionId.ToString(),
                        QuestionType = q.QuestionType,
                        QuestionContent = q.QuestionContent,
                        Difficulty = q.Difficulty,
                        Answers = answers
                    };
                }).ToList();

                // Cache trong 5 phút
                _cache.Set(cacheKey, questions, TimeSpan.FromMinutes(5));
            }

            // Clone list để shuffle độc lập cho mỗi sinh viên (nếu cần)
            var questionsForStudent = questions.ToList();

            // 5. Lấy thông tin Exam (Cache theo PaperId hoặc ExamId)
            string examCacheKey = $"ExamMeta_Paper_{activeSubmission.PaperId}";
            if (!_cache.TryGetValue(examCacheKey, out (int ExamId, int Duration, int Code, bool ShuffleQuestion) meta))
            {
                var paperMeta = await _studentExamRepository.GetPaperWithExamAsync(activeSubmission.PaperId);
                if (paperMeta?.Exam == null) return null;
                meta = (paperMeta.Exam.ExamId, paperMeta.Exam.Duration, paperMeta.Code, paperMeta.Exam.ShuffleQuestion);
                _cache.Set(examCacheKey, meta, TimeSpan.FromMinutes(5));
            }

            // 6. Shuffle questions
            if (meta.ShuffleQuestion)
            {
                questionsForStudent.Shuffle();
            }

            // 7. Trả về TakeExamDto
            return new TakeExamDto
            {
                ExamId = meta.ExamId.ToString(),
                SubmissionId = activeSubmission.SubmissionId.ToString(),
                Duration = meta.Duration,
                Code = meta.Code,
                Questions = questionsForStudent
            };
        }

        public async Task<ExamPreviewDto?> GetExamPreviewAsync(int userId, int examId, bool isTeacher = false)
        {
            if (!isTeacher)
            {
                var canTake = await _studentExamRepository.CanStudentTakeExamAsync(userId, examId);
                if (!canTake)
                {
                    throw new UnauthorizedAccessException("Bạn không thuộc lớp được chỉ định để xem bài thi này.");
                }
            }

            var data = await _studentExamRepository.GetExamPreviewAsync(examId);
            if (data == null) return null;

            var statusLabel = data.Status switch
            {
                ExamStatus.Ready => "pending",
                ExamStatus.Published => "public",
                ExamStatus.InProgress => "inprogress",
                ExamStatus.Deleted => "deleted",
                ExamStatus.Cancelled => "cancelled",
                ExamStatus.Closed => "closed",
                _ => "unknown"
            };

            var matrixRows = isTeacher ? data.BlueprintChapters
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
                .ToList() : new List<BlueprintRowDto>();

            var studentAttempts = isTeacher ? 0 : await _studentExamRepository.GetExamSubmissionCountAsync(userId, examId);
            var remainingAttempts = isTeacher ? 0 : Math.Max(0, data.MaxAttempts - studentAttempts);

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
                MaxAttempts = data.MaxAttempts,
                RemainingAttempts = remainingAttempts,
                ShowScore = data.ShowScore,
                ShowAnswer = data.ShowAnswer,
                AnswerTimingMode = data.AnswerTimingMode,
                PaperCount = data.PaperCount,
                BlueprintMatrix = matrixRows
            };
        }
    }
}
