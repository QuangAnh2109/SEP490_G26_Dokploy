using Backend.Constants;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Backend.Jobs;

public sealed class ExamScheduleRehydrator(
    IServiceScopeFactory scopeFactory,
    ILogger<ExamScheduleRehydrator> logger,
    IExamStatusScheduler scheduler,
    IConnectionMultiplexer mux,
    TimeProvider timeProvider) : IHostedService
{
    /// <summary>
    /// Khi app khởi động: rescan exam Published/InProgress với CloseAt &gt; now và reschedule
    /// Hangfire jobs cho mỗi exam. Distributed Redis lock đảm bảo chỉ 1 replica chạy
    /// (tránh race tracker keys + duplicate Hangfire enqueue trên multi-instance).
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var redis = mux.GetDatabase(RedisKeys.HangfireDb);
        var lockId = Guid.NewGuid().ToString("N");

        // Distributed lock SETNX TTL 2 phút — đủ cho rehydrate ~1000 exam;
        // nếu replica crash giữa chừng, lock auto-expire.
        var acquired = await redis.StringSetAsync(
            RedisKeys.RehydratorLockKey, lockId, TimeSpan.FromMinutes(2), When.NotExists);
        if (!acquired)
        {
            logger.LogInformation("ExamScheduleRehydrator: another instance holds the lock, skipping.");
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MtcaSep490G26Context>();

            var now = timeProvider.GetUtcNow().UtcDateTime;
            // Cover cả 2 status có thể bị stuck sau downtime: Published (chưa qua Open) và InProgress (chưa qua Close).
            // CloseAt là milestone cuối — đã qua nghĩa là exam đã xong, không cần rehydrate.
            var exams = await db.Exams
                .Where(e => (e.Status == ExamStatus.Published || e.Status == ExamStatus.InProgress)
                            && e.CloseAt > now)
                .Select(e => new { e.ExamId, e.OpenAt, e.CloseAt })
                .ToListAsync(cancellationToken);

            foreach (var exam in exams)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // ScheduleExamJobsAsync idempotent + clamp delay âm về 0 (catch-up khi mốc đã qua).
                await scheduler.ScheduleExamJobsAsync(exam.ExamId, exam.OpenAt, exam.CloseAt, cancellationToken);
                logger.LogDebug(
                    "ExamScheduleRehydrator: rescheduled jobs for Exam {ExamId} (OpenAt={OpenAt}, CloseAt={CloseAt})",
                    exam.ExamId, exam.OpenAt, exam.CloseAt);
            }

            logger.LogInformation("ExamScheduleRehydrator: rescheduled {Count} exam(s).", exams.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Swallow: app phải start được kể cả khi rehydrate fail (DB chậm, Redis blip, ...).
            // Lock TTL 2 phút sẽ expire — restart kế tiếp sẽ retry rehydrate.
            // Trade-off: trong khoảng đó, exam đến mốc OpenAt/CloseAt sẽ KHÔNG transition.
            logger.LogError(ex,
                "ExamScheduleRehydrator: failed to rehydrate exam jobs; app continues startup, lock will expire in 2 minutes.");
        }
        finally
        {
            // Release lock chỉ khi vẫn là của mình (tránh xóa lock của replica khác nếu TTL đã expire).
            var current = await redis.StringGetAsync(RedisKeys.RehydratorLockKey);
            if (current == lockId)
            {
                await redis.KeyDeleteAsync(RedisKeys.RehydratorLockKey);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
