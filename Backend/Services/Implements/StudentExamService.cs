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
                    QuestionContent = q.QuestionContent,
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
                Duration = paper.Exam.Duration,
                Code = paper.Code,
                Questions = questions
            };
        }
    }
}
