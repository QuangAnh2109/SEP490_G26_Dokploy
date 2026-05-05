namespace Backend.Jobs;

public interface IExamStatusScheduler
{
    /// <summary>
    /// Schedule (hoặc reschedule) Hangfire jobs để chuyển exam qua các mốc Published → InProgress → Closed
    /// theo OpenAt/CloseAt. Idempotent: thay tracker key + xóa job Hangfire cũ trước khi enqueue mới.
    /// Mốc đã qua được clamp về delay 0 (catch-up).
    /// </summary>
    /// <remarks>
    /// Side effects: 0–2 Hangfire enqueues + 0–2 Redis writes (tracker keys
    /// <c>mtca:exam-jobs:{id}:open|close</c>).
    /// An toàn với concurrent calls — atomic GETSET trên tracker đảm bảo mỗi caller xóa đúng job mà nó replace.
    /// Caller phải persist exam row trước khi gọi (job sẽ load exam qua DbContext).
    /// </remarks>
    Task ScheduleExamJobsAsync(int examId, DateTime? openAtUtc, DateTime? closeAtUtc, CancellationToken ct = default);

    /// <summary>
    /// Hủy mọi pending Hangfire job cho exam và xóa tracker key tương ứng.
    /// Idempotent: an toàn khi gọi mà không có job nào đang schedule.
    /// </summary>
    Task CancelExamJobsAsync(int examId, CancellationToken ct = default);
}
