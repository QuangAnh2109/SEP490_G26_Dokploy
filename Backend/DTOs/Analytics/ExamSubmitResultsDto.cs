using System;
using System.Collections.Generic;

namespace Backend.DTOs.Analytics;

/// <summary>
/// DTO cho trang thống kê nộp bài - Giáo viên xem danh sách học sinh và lịch sử nộp bài.
/// </summary>
public class ExamSubmitResultsDto
{
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = null!;
    public string? ClassName { get; set; }
    public int DurationMinutes { get; set; }
    public int MaxAttempts { get; set; }
    public int TotalStudents { get; set; }
    public int SubmittedCount { get; set; }
    public List<StudentSubmitItemDto> Students { get; set; } = new();
}

public class StudentSubmitItemDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = null!;  // MSSV
    public string FullName { get; set; } = null!;
    public DateTime? LastSubmitAt { get; set; }
    public string? DurationFormatted { get; set; }     // e.g. "45p 12s"
    public decimal? LastScore { get; set; }
    public int AttemptCount { get; set; }
    public string Status { get; set; } = null!;       // Đã nộp | Đang làm | Vắng thi
    public List<SubmissionHistoryDto> History { get; set; } = new();
}

public class SubmissionHistoryDto
{
    public int SubmissionId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string DurationFormatted { get; set; } = null!;
    public decimal? Score { get; set; }
    public bool IsLast { get; set; }
}
