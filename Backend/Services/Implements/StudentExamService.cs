using System.Text.Json;
using System.Text.Json.Nodes;

using Backend.Constants;
using Backend.DTOs.StudentExam;
using Backend.Helpers;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

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

            // 4. Lấy paper với questions, answers, input types
            var paper = await _studentExamRepository.GetPaperWithQuestionsAsync(examId, activeSubmission.PaperId);
            if (paper == null || paper.Exam == null)
            {
                return null;
            }

            // 5. Map sang TakeExamQuestionDto
            var questions = paper.Questions.Select(q =>
            {
                // Map answers
                var answers = q.QuestionAnswers.Select(qa => new TakeExamAnswerDto
                {
                    QuestionAnswerId = qa.QuestionAnswerId.ToString(),
                    Content = qa.Content,
                    GroupAnswerId = qa.GroupAnswerId.HasValue
                        ? qa.GroupAnswerId.Value.ToString()
                        : null,
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

            // 6. Shuffle questions
            if (paper.Exam.ShuffleQuestion)
            {
                questions.Shuffle();
            }

            // 7. Trả về TakeExamDto
            return new TakeExamDto
            {
                ExamId = paper.Exam.ExamId.ToString(),
                SubmissionId = activeSubmission.SubmissionId.ToString(),
                Duration = paper.Exam.Duration,
                Code = paper.Code,
                Questions = questions
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
