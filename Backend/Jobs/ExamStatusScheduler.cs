using Backend.Constants;
using Hangfire;
using StackExchange.Redis;

namespace Backend.Jobs;

public sealed class ExamStatusScheduler(
    IConnectionMultiplexer mux,
    IBackgroundJobClient jobClient,
    TimeProvider timeProvider) : IExamStatusScheduler
{
    // Grace period sau mốc fire để CancelExamJobsAsync vẫn xóa được tracker
    // trong race với job đang fire (job hoàn tất nhưng tracker chưa expire).
    private static readonly TimeSpan JobIdGracePeriod = TimeSpan.FromHours(1);

    private static string OpenKey(int examId) => $"{RedisKeys.ExamJobsPrefix}:{examId}:open";
    private static string CloseKey(int examId) => $"{RedisKeys.ExamJobsPrefix}:{examId}:close";

    private readonly IBackgroundJobClient _jobClient = jobClient;
    private readonly IDatabase _redis = mux.GetDatabase(RedisKeys.HangfireDb);

    public async Task ScheduleExamJobsAsync(int examId, DateTime? openAtUtc, DateTime? closeAtUtc, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        await ScheduleOrClearAsync(
            OpenKey(examId),
            openAtUtc,
            now,
            delay => _jobClient.Schedule<ExamStatusTransitionJob>(
                job => job.TransitionToInProgress(examId, JobCancellationToken.Null), delay));

        ct.ThrowIfCancellationRequested();

        await ScheduleOrClearAsync(
            CloseKey(examId),
            closeAtUtc,
            now,
            delay => _jobClient.Schedule<ExamStatusTransitionJob>(
                job => job.TransitionToClosed(examId, JobCancellationToken.Null), delay));
    }

    public async Task CancelExamJobsAsync(int examId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await CancelTrackerAsync(OpenKey(examId));
        await CancelTrackerAsync(CloseKey(examId));
    }

    private async Task ScheduleOrClearAsync(string key, DateTime? milestoneUtc, DateTime now, Func<TimeSpan, string> schedule)
    {
        if (!milestoneUtc.HasValue)
        {
            await _redis.KeyDeleteAsync(key);
            return;
        }

        var delay = milestoneUtc.Value > now ? milestoneUtc.Value - now : TimeSpan.Zero;
        var newJobId = schedule(delay);

        // Atomic GETSET: ghi tracker = newJobId, trả về oldJobId của lần schedule trước (nếu có).
        // An toàn với concurrent calls — last writer wins, mỗi caller tự xóa job mà nó replace.
        var oldJobId = await _redis.StringGetSetAsync(key, newJobId);
        await _redis.KeyExpireAsync(key, delay + JobIdGracePeriod);

        if (!oldJobId.IsNullOrEmpty) _jobClient.Delete(oldJobId!);
    }

    private async Task CancelTrackerAsync(string key)
    {
        var jobId = await _redis.StringGetAsync(key);
        if (jobId.IsNullOrEmpty) return;

        _jobClient.Delete(jobId!);
        await _redis.KeyDeleteAsync(key);
    }
}
