using Backend.Constants;
using Backend.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Backend.Jobs;

public sealed class ExamStatusTransitionJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ExamStatusTransitionJob> logger,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Hangfire job: chuyển exam Published → InProgress khi tới mốc OpenAt.
    /// </summary>
    /// <remarks>
    /// Idempotent qua status check + Exam.ConcurrencyStamp (rowversion). An toàn để retry.
    /// Race với teacher Cancel được catch ở DbUpdateConcurrencyException → log Warning + return,
    /// không bubble (tránh Hangfire retry storm 10 lần).
    /// </remarks>
    public async Task TransitionToInProgress(int examId, IJobCancellationToken jobToken)
    {
        var ct = jobToken.ShutdownToken;
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MtcaSep490G26Context>();

        var exam = await db.Exams.FindAsync(new object[] { examId }, ct);
        if (exam == null)
        {
            logger.LogWarning("ExamStatusTransitionJob: Exam {ExamId} not found, skipping InProgress transition.", examId);
            return;
        }

        if (exam.Status != ExamStatus.Published)
        {
            logger.LogDebug("ExamStatusTransitionJob: Exam {ExamId} is not Published (Status={Status}), skipping InProgress transition.", examId, exam.Status);
            return;
        }

        exam.Status = ExamStatus.InProgress;
        exam.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("ExamStatusTransitionJob: Concurrency conflict on Exam {ExamId} during InProgress transition; will resolve next tick.", examId);
            return;
        }

        logger.LogInformation("ExamStatusTransitionJob: Exam {ExamId} transitioned Published → InProgress.", examId);
    }

    /// <summary>
    /// Hangfire job: chuyển exam → Closed khi tới mốc CloseAt. Converging state machine — chấp nhận
    /// cả Published (miss-window: Open job mất hoặc fire sau Close) và InProgress (luồng bình thường).
    /// </summary>
    /// <remarks>
    /// Idempotent qua status check + Exam.ConcurrencyStamp. An toàn để retry.
    /// Status khác (Cancelled/Closed/Ready) → skip.
    /// </remarks>
    public async Task TransitionToClosed(int examId, IJobCancellationToken jobToken)
    {
        var ct = jobToken.ShutdownToken;
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MtcaSep490G26Context>();

        var exam = await db.Exams.FindAsync(new object[] { examId }, ct);
        if (exam == null)
        {
            logger.LogWarning("ExamStatusTransitionJob: Exam {ExamId} not found, skipping Closed transition.", examId);
            return;
        }

        if (exam.Status != ExamStatus.Published && exam.Status != ExamStatus.InProgress)
        {
            logger.LogDebug("ExamStatusTransitionJob: Exam {ExamId} is not Published/InProgress (Status={Status}), skipping Closed transition.", examId, exam.Status);
            return;
        }

        exam.Status = ExamStatus.Closed;
        exam.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("ExamStatusTransitionJob: Concurrency conflict on Exam {ExamId} during Closed transition; will resolve next tick.", examId);
            return;
        }

        logger.LogInformation("ExamStatusTransitionJob: Exam {ExamId} transitioned → Closed.", examId);
    }
}
