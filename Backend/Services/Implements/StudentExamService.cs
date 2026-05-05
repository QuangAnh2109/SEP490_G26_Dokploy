using System.Text.Json;
using System.Text.Json.Nodes;

using Backend.Constants;
using Backend.DTOs.StudentExam;
using Backend.Common;
using Backend.Common.Models;
using Backend.Common.Errors;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services.Implements;

public class StudentExamService(
    IStudentExamRepository studentExamRepository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider) : IStudentExamService
{
    public async Task<Result<TakeExamDto>> TakeExamInClass(int examId)
    {
        var studentId = currentUserService.UserId;

        // 0. Tự động nộp bài cho các submission đã quá thời gian
        await studentExamRepository.ForceSubmitOverdueExamsAsync(examId);

        // 1. Kiểm tra exam tồn tại, student thuộc lớp, thời gian hợp lệ
        var examInfo = await studentExamRepository.GetExamInfoForStudentAsync(examId, studentId);
        if (examInfo == null)
        {
            return StudentExamErrors.NotFound;
        }

        // 2. Kiểm tra submission hiện tại của học sinh
        Submission? activeSubmission = null;
        var anyActiveSubmission = await studentExamRepository.GetAnyActiveSubmissionAsync(studentId);

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
                return StudentExamErrors.AnotherActiveSubmission;
            }
        }

        // 3. Nếu không có submission active → tạo mới
        if (activeSubmission == null)
        {
            // Kiểm tra MaxAttempts  
            if (examInfo.MaxAttempts > 0 && examInfo.StudentAttempts >= examInfo.MaxAttempts)
            {
                return StudentExamErrors.MaxAttemptsReached;
            }

            // Random chọn PaperId
            if (examInfo.PaperIds == null || examInfo.PaperIds.Count == 0)
            {
                return StudentExamErrors.NoPapers;
            }

            var random = new Random();
            int randomIndex = random.Next(examInfo.PaperIds.Count);
            int selectedPaperId = examInfo.PaperIds[randomIndex];

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var newSubmission = new Submission
            {
                StudentId = studentId,
                PaperId = selectedPaperId,
                Status = SubmissionStatus.InProgress,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            activeSubmission = await studentExamRepository.CreateSubmissionAsync(newSubmission);
        }

        // 4. Lấy paper với questions, answers, input types
        var paper = await studentExamRepository.GetPaperWithQuestionsAsync(examId, activeSubmission.PaperId);
        if (paper == null || paper.Exam == null)
        {
            return StudentExamErrors.NotFound;
        }

        // 5. Map sang TakeExamQuestionDto
        var questions = paper.Questions.Select(q =>
        {
            // Map answers
            var answers = q.QuestionAnswers.Select(qa => new TakeExamAnswerDto
            {
                QuestionAnswerId = qa.QuestionAnswerId,
                Content = qa.Content,
                GroupAnswerId = qa.GroupAnswerId,
                InputTypes = qa.BlankInputs.Select(bi => new TakeExamInputTypeDto
                {
                    InputTypeId = bi.InputTypeId,
                    Name = bi.InputType.Name,
                    Regex = bi.InputType.Regex,
                    GroupType = bi.InputType.GroupType
                }).ToList()
            }).ToList();

            return new TakeExamQuestionDto
            {
                QuestionId = q.QuestionId,
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

        // 7. Lấy câu trả lời đã lưu trước đó (nếu có)
        var savedAnswers = activeSubmission.StudentAnswers?
            .Select(sa => new TakeExamSavedAnswerDto
            {
                QuestionAnswerId = sa.QuestionAnswerId,
                Response = sa.Response
            }).ToList() ?? new List<TakeExamSavedAnswerDto>();

        // 8. Trả về TakeExamDto
        return new TakeExamDto
        {
            ExamId = paper.Exam.ExamId.ToString(),
            SubmissionId = activeSubmission.SubmissionId.ToString(),
            Duration = paper.Exam.Duration,
            Code = paper.Code ?? 0,
            CreatedAtUtc = activeSubmission.CreatedAtUtc,
            Questions = questions,
            SavedAnswers = savedAnswers
        };
    }

    public async Task<Result<ExamPreviewDto>> GetExamPreviewAsync(int examId)
    {
        var userId = currentUserService.UserId;
        var isTeacher = currentUserService.Role == RoleIds.Teacher;

        if (!isTeacher)
        {
            var canTake = await studentExamRepository.CanStudentTakeExamAsync(userId, examId);
            if (!canTake)
            {
                return StudentExamErrors.NotAllowed;
            }
        }

        var data = await studentExamRepository.GetExamPreviewAsync(examId);
        if (data == null) return StudentExamErrors.NotFound;

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

    // ════════════════════════════════════════════════════════
    //  LỊCH SỬ BÀI NỘP TỔNG HỢP (Kiểm tra + Luyện tập)
    // ════════════════════════════════════════════════════════
    public async Task<Result<List<StudentSubmissionHistoryDto>>> GetAllSubmissionHistoryAsync(int? classId = null)
    {
        var studentId = currentUserService.UserId;
        var rawList = await studentExamRepository.GetSubmissionHistoryRawAsync(studentId, classId);

        var result = rawList.Select(r => new StudentSubmissionHistoryDto
        {
            SubmissionId = r.SubmissionId,
            Type = r.IsExam ? "Kiểm tra" : "Luyện tập",
            Title = r.Title,
            ClassName = r.ClassName,
            SubjectName = r.SubjectName,
            TotalQuestions = r.TotalQuestions,
            TotalPoints = r.TotalPoints,
            Status = r.Status == SubmissionStatus.Submitted ? "Đã nộp" : "Đang làm",
            CreatedAtUtc = r.CreatedAtUtc,
            CompletedAtUtc = r.Status == SubmissionStatus.Submitted ? r.UpdatedAtUtc : null,
            ExamId = r.ExamId
        }).ToList();

        return result;
    }
}
