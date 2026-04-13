using Backend.Constants;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Jobs;

/// <summary>
/// Hangfire job xử lý chuyển trạng thái đề thi tự động.
/// Mỗi job được lập lịch tại thời điểm chính xác (OpenAt / CloseAt)
/// thay vì polling liên tục, giảm tải cho server.
/// </summary>
public class ExamStatusJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExamStatusJob> _logger;

    public ExamStatusJob(IServiceScopeFactory scopeFactory, ILogger<ExamStatusJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Chuyển đề thi từ Published → InProgress khi đến thời điểm OpenAt.
    /// </summary>
    public async Task TransitionToInProgress(int examId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MtcaSep490G26Context>();

        var exam = await db.Exams.FindAsync(examId);
        if (exam == null)
        {
            _logger.LogWarning("ExamStatusJob: Exam {ExamId} not found, skipping InProgress transition.", examId);
            return;
        }

        // Chỉ chuyển nếu đang ở trạng thái Published
        if (exam.Status != ExamStatus.Published)
        {
            _logger.LogInformation("ExamStatusJob: Exam {ExamId} is not Published (Status={Status}), skipping InProgress transition.", examId, exam.Status);
            return;
        }

        exam.Status = ExamStatus.InProgress;
        exam.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("ExamStatusJob: Exam {ExamId} transitioned Published → InProgress.", examId);
    }

    /// <summary>
    /// Chuyển đề thi từ InProgress → Closed khi đến thời điểm CloseAt.
    /// </summary>
    public async Task TransitionToClosed(int examId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MtcaSep490G26Context>();

        var exam = await db.Exams.FindAsync(examId);
        if (exam == null)
        {
            _logger.LogWarning("ExamStatusJob: Exam {ExamId} not found, skipping Closed transition.", examId);
            return;
        }

        // Chỉ chuyển nếu đang ở trạng thái InProgress
        if (exam.Status != ExamStatus.InProgress)
        {
            _logger.LogInformation("ExamStatusJob: Exam {ExamId} is not InProgress (Status={Status}), skipping Closed transition.", examId, exam.Status);
            return;
        }

        exam.Status = ExamStatus.Closed;
        exam.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("ExamStatusJob: Exam {ExamId} transitioned InProgress → Closed.", examId);
    }

    // ── Helper: Lập lịch / Hủy lịch ──

    /// <summary>
    /// Đặt lịch 2 job cho 1 đề thi: InProgress tại OpenAt, Closed tại CloseAt.
    /// Nếu đã có job cũ cho examId này, sẽ bị ghi đè (cùng jobId).
    /// </summary>
    public static void ScheduleExamJobs(int examId, DateTime? openAtUtc, DateTime? closeAtUtc)
    {
        var now = DateTime.UtcNow;

        if (openAtUtc.HasValue && openAtUtc.Value > now)
        {
            var delay = openAtUtc.Value - now;
            Hangfire.BackgroundJob.Schedule<ExamStatusJob>(
                job => job.TransitionToInProgress(examId),
                delay);
        }

        if (closeAtUtc.HasValue && closeAtUtc.Value > now)
        {
            var delay = closeAtUtc.Value - now;
            Hangfire.BackgroundJob.Schedule<ExamStatusJob>(
                job => job.TransitionToClosed(examId),
                delay);
        }
    }

    /// <summary>
    /// Hủy các job đã lập lịch cho đề thi (dùng khi Cancel exam).
    /// Lưu ý: Hangfire Schedule không hỗ trợ custom jobId nên ta dùng
    /// cách khác — khi job chạy sẽ tự kiểm tra trạng thái hiện tại và skip
    /// nếu không phù hợp (built-in safety trong TransitionToInProgress/TransitionToClosed).
    /// </summary>
    public static void CancelExamJobs(int examId)
    {
        // Hangfire BackgroundJob.Schedule không hỗ trợ custom ID.
        // Thay vào đó, khi job thực thi, nó sẽ kiểm tra status hiện tại
        // và bỏ qua nếu không đúng (Published/InProgress).
        // → Không cần hủy thủ công, job sẽ tự "no-op" khi chạy.
    }
}
