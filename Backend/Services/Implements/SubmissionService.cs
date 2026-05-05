using Backend.Common;
using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Constants;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Backend.Helpers;

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

        // ── 2. Tính cờ nộp trễ (TimeProvider) ──────────
        // Vẫn lưu submission khi quá giờ — auto-submit FE bắn đúng lúc hết giờ
        // luôn đến BE trễ vài ms; nếu reject, submission kẹt InProgress mãi mãi.
        // FE đọc IsLate trong response để hiện toast "Nộp bài trễ".
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var startTime = submission.CreatedAtUtc;
        var deadline = startTime.AddMinutes(exam.Duration);

        bool isLate = now > deadline || (exam.CloseAt.HasValue && now > exam.CloseAt.Value);

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
        var incomingData = (request.StudentAnswers ?? [])
            .Where(sa => sa.QuestionAnswerId.HasValue)
            .Select(sa => (sa.QuestionAnswerId!.Value, sa.Response));

        StudentAnswerSyncHelper.SyncAnswers(
            submission.StudentAnswers, 
            incomingData, 
            submission.SubmissionId);

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
