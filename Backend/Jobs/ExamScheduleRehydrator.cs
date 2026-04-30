using Backend.Constants;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Jobs;

public sealed class ExamScheduleRehydrator(
    IServiceScopeFactory scopeFactory,
    ILogger<ExamScheduleRehydrator> logger,
    ExamStatusJob examStatusJob) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MtcaSep490G26Context>();

        var now = DateTime.UtcNow;
        var exams = await db.Exams
            .Where(e => e.Status == ExamStatus.Published && e.OpenAt > now)
            .Select(e => new { e.ExamId, e.OpenAt, e.CloseAt })
            .ToListAsync(cancellationToken);

        foreach (var exam in exams)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // ScheduleExamJobsAsync xóa job cũ trong Redis trước khi tạo job mới (idempotent)
            await examStatusJob.ScheduleExamJobsAsync(exam.ExamId, exam.OpenAt, exam.CloseAt, cancellationToken);
            logger.LogDebug(
                "ExamScheduleRehydrator: rescheduled jobs for Exam {ExamId} (OpenAt={OpenAt})",
                exam.ExamId, exam.OpenAt);
        }

        logger.LogInformation(
            "ExamScheduleRehydrator: rescheduled {Count} exam(s).",
            exams.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
