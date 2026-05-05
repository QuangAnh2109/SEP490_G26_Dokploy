namespace Backend.Services.Interfaces;

public interface IGradingService
{
    /// <summary>
    /// Chấm điểm submission đồng bộ. Idempotent: gọi lại trên submission đã Graded sẽ no-op.
    /// KHÔNG nhận CancellationToken — luồng chấm điểm phải hoàn tất kể cả khi client cancel
    /// HTTP request, để tránh kẹt submission ở trạng thái dở.
    /// </summary>
    Task GradeSubmissionAsync(int submissionId);
}
