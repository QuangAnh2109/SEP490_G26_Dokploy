using Backend.Constants;
using Backend.Models;
using Hangfire;
using StackExchange.Redis;

namespace Backend.Jobs;

public class ExamStatusJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ExamStatusJob> logger,
    IConnectionMultiplexer mux,
    IBackgroundJobClient jobClient)
{
    private static readonly TimeSpan JobIdGracePeriod = TimeSpan.FromHours(1);

    private static string OpenKey(int examId) => $"{RedisKeys.ExamJobsPrefix}:{examId}:open";
    private static string CloseKey(int examId) => $"{RedisKeys.ExamJobsPrefix}:{examId}:close";

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<ExamStatusJob> _logger = logger;
    private readonly IBackgroundJobClient _jobClient = jobClient;
    private readonly IDatabase _redis = mux.GetDatabase(RedisKeys.HangfireDb);

    public async Task TransitionToInProgress(int examId, IJobCancellationToken jobToken)
    {
        var ct = jobToken.ShutdownToken;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MtcaSep490G26Context>();

        var exam = await db.Exams.FindAsync(new object[] { examId }, ct);
        if (exam == null)
        {
            _logger.LogWarning("ExamStatusJob: Exam {ExamId} not found, skipping InProgress transition.", examId);
            return;
        }

        if (exam.Status != ExamStatus.Published)
        {
            _logger.LogInformation("ExamStatusJob: Exam {ExamId} is not Published (Status={Status}), skipping.", examId, exam.Status);
            return;
        }

        exam.Status = ExamStatus.InProgress;
        exam.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("ExamStatusJob: Exam {ExamId} transitioned Published → InProgress.", examId);
    }

    public async Task TransitionToClosed(int examId, IJobCancellationToken jobToken)
    {
        var ct = jobToken.ShutdownToken;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MtcaSep490G26Context>();

        var exam = await db.Exams.FindAsync(new object[] { examId }, ct);
        if (exam == null)
        {
            _logger.LogWarning("ExamStatusJob: Exam {ExamId} not found, skipping Closed transition.", examId);
            return;
        }

        if (exam.Status != ExamStatus.InProgress)
        {
            _logger.LogInformation("ExamStatusJob: Exam {ExamId} is not InProgress (Status={Status}), skipping.", examId, exam.Status);
            return;
        }

        exam.Status = ExamStatus.Closed;
        exam.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("ExamStatusJob: Exam {ExamId} transitioned InProgress → Closed.", examId);
    }

    // Job ID được track trong Redis db 1 (cùng db với Hangfire) theo key:
    //   mtca:exam-jobs:{examId}:open   → Hangfire job ID cho TransitionToInProgress
    //   mtca:exam-jobs:{examId}:close  → Hangfire job ID cho TransitionToClosed
    // Mỗi lần schedule lại → xóa job cũ trước, tránh tích lũy job trùng khi app restart.

    public async Task ScheduleExamJobsAsync(int examId, DateTime? openAtUtc, DateTime? closeAtUtc, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var now = DateTime.UtcNow;
        var openKey = OpenKey(examId);
        var closeKey = CloseKey(examId);

        var oldOpen = await _redis.StringGetAsync(openKey);
        if (!oldOpen.IsNullOrEmpty) _jobClient.Delete(oldOpen!);

        var oldClose = await _redis.StringGetAsync(closeKey);
        if (!oldClose.IsNullOrEmpty) _jobClient.Delete(oldClose!);

        ct.ThrowIfCancellationRequested();

        if (openAtUtc.HasValue && openAtUtc.Value > now)
        {
            var delay = openAtUtc.Value - now;
            var jobId = _jobClient.Schedule<ExamStatusJob>(
                job => job.TransitionToInProgress(examId, JobCancellationToken.Null), delay);
            await _redis.StringSetAsync(openKey, jobId, delay + JobIdGracePeriod);
        }
        else
        {
            await _redis.KeyDeleteAsync(openKey);
        }

        if (closeAtUtc.HasValue && closeAtUtc.Value > now)
        {
            var delay = closeAtUtc.Value - now;
            var jobId = _jobClient.Schedule<ExamStatusJob>(
                job => job.TransitionToClosed(examId, JobCancellationToken.Null), delay);
            await _redis.StringSetAsync(closeKey, jobId, delay + JobIdGracePeriod);
        }
        else
        {
            await _redis.KeyDeleteAsync(closeKey);
        }
    }

    public async Task CancelExamJobsAsync(int examId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var openKey = OpenKey(examId);
        var closeKey = CloseKey(examId);

        var openId = await _redis.StringGetAsync(openKey);
        if (!openId.IsNullOrEmpty)
        {
            _jobClient.Delete(openId!);
            await _redis.KeyDeleteAsync(openKey);
        }

        var closeId = await _redis.StringGetAsync(closeKey);
        if (!closeId.IsNullOrEmpty)
        {
            _jobClient.Delete(closeId!);
            await _redis.KeyDeleteAsync(closeKey);
        }
    }
}
