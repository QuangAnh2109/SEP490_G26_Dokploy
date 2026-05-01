using Backend.Common;
using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Constants;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services.Implements;

public class SubmissionService(
    ISubmissionRepository submissionRepo,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider) : ISubmissionService
{
    public async Task<Result<SubmitExamResponse>> SubmitExamAsync(
        SubmitExamRequest request,
        CancellationToken ct = default)
    {
        var studentId = currentUserService.UserId;

        // ── 1. Tìm Submission đang InProgress ──────────────────────────
        var submission = await submissionRepo.GetActiveSubmissionAsync(
            request.ExamId!.Value, studentId, ct);

        if (submission == null)
            return SubmissionErrors.NotFound;

        if (submission.Status != SubmissionStatus.InProgress)
            return SubmissionErrors.AlreadySubmitted;

        var exam = submission.Paper?.Exam;
        if (exam == null)
            return SubmissionErrors.NotFound;

        // ── 2. Kiểm tra thời gian (TimeProvider) ──────────
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var startTime = submission.CreatedAtUtc;             // thời gian bắt đầu làm bài
        var deadline = startTime.AddMinutes(exam.Duration);  // hết giờ theo duration

        bool isLate = now > deadline || (exam.CloseAt.HasValue && now > exam.CloseAt.Value);

        if (isLate)
            return SubmissionErrors.Late;

        // ── 3. Validate QuestionAnswerIds thuộc Paper ────────────────────
        var validIds = await submissionRepo.GetValidQuestionAnswerIdsAsync(
            submission.PaperId, ct);

        if (request.StudentAnswers != null)
        {
            foreach (var sa in request.StudentAnswers)
            {
                if (sa.QuestionAnswerId.HasValue && !validIds.Contains(sa.QuestionAnswerId.Value))
                    return SubmissionErrors.InvalidAnswer;
            }
        }

        // ── 4. Xử lý StudentAnswers ────────────────────────────────────
        var existingAnswers = submission.StudentAnswers.ToList();
        var incomingAnswers = request.StudentAnswers ?? [];

        // Tập hợp QuestionAnswerId mới từ request
        var incomingIds = new HashSet<int>(
            incomingAnswers.Where(sa => sa.QuestionAnswerId.HasValue)
                           .Select(sa => sa.QuestionAnswerId!.Value));

        // Map QuestionAnswerId → dto cho tra cứu nhanh
        var incomingMap = incomingAnswers
            .Where(sa => sa.QuestionAnswerId.HasValue)
            .ToDictionary(sa => sa.QuestionAnswerId!.Value);

        // Map QuestionAnswerId → existing StudentAnswer
        var existingMap = existingAnswers
            .ToDictionary(sa => sa.QuestionAnswerId);

        // 4a. Xóa những bản ghi cũ không còn trong request (chỉ áp dụng MCQ)
        var toRemove = existingAnswers
            .Where(sa => !incomingIds.Contains(sa.QuestionAnswerId))
            .ToList();

        // 4b. Thêm bản ghi mới chưa tồn tại
        var toAdd = new List<StudentAnswer>();

        // 4c. Update bản ghi đã tồn tại (FillInBlank: cập nhật Response)
        foreach (var dto in incomingAnswers.Where(sa => sa.QuestionAnswerId.HasValue))
        {
            if (existingMap.TryGetValue(dto.QuestionAnswerId!.Value, out var existing))
            {
                // Đã tồn tại → update Response (chủ yếu cho FillInBlank)
                existing.Response = dto.Response;
            }
            else
            {
                // Chưa tồn tại → thêm mới
                toAdd.Add(new StudentAnswer
                {
                    SubmissionId = submission.SubmissionId,
                    QuestionAnswerId = dto.QuestionAnswerId.Value,
                    Response = dto.Response
                });
            }
        }

        if (toRemove.Count > 0)
            submissionRepo.RemoveStudentAnswers(toRemove);

        if (toAdd.Count > 0)
            submissionRepo.AddStudentAnswers(toAdd);

        // ── 5. Cập nhật Submission ──────────────────────────────────────
        submission.Status = request.Submit == true ? 2 : 1;
        submission.UpdatedAtUtc = now;

        await submissionRepo.SaveChangesAsync(ct);

        // ── 6. Return response ──────────────────────────────────────────
        return new SubmitExamResponse(
            submission.SubmissionId,
            now,
            isLate
        );
    }
}
