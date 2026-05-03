namespace Backend.Constants;

/// <summary>
/// Trạng thái chấm điểm cho một <see cref="Models.Submission"/>.
/// </summary>
public static class GradingStatus
{
    /// <summary>Đã nộp bài nhưng chưa start grading job.</summary>
    public const byte NotGraded = 0;

    /// <summary>Job đang chạy (MCQ + AEC pending).</summary>
    public const byte InProgress = 1;

    /// <summary>Đã chấm xong cả 2 nhánh — TotalPoints đáng tin.</summary>
    public const byte Graded = 2;

    /// <summary>Job catch exception — admin can thiệp tay.</summary>
    public const byte Failed = 3;

    public static bool IsTerminal(byte status) => status == Graded || status == Failed;
}
