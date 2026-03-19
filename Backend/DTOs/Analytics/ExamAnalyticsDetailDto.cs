using System.Collections.Generic;

namespace Backend.DTOs.Analytics;

/// <summary>
/// DTO phân tích chi tiết bài thi — dành cho Giáo viên.
/// Chứa thống kê tổng quan, phân bố điểm, phân tích theo chương + độ khó,
/// danh sách HS, và đề xuất cải thiện tự động.
/// </summary>
public class ExamAnalyticsDetailDto
{
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = null!;
    public int TotalSubmissions { get; set; }

    // ── Thống kê tổng quan ──
    public decimal AverageScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal MinScore { get; set; }
    public decimal MedianScore { get; set; }

    // ── Phân bố điểm (key: khoảng điểm, value: số HS) ──
    public Dictionary<string, int> ScoreDistribution { get; set; } = new();

    // ── Phân tích theo chương ──
    public List<ChapterAnalyticsDto> ChapterStats { get; set; } = new();

    // ── Phân tích theo độ khó ──
    public List<DifficultyAnalyticsDto> DifficultyStats { get; set; } = new();

    // ── Top câu hỏi khó nhất ──
    public List<HardestQuestionDto> HardestQuestions { get; set; } = new();

    // ── Danh sách kết quả từng HS ──
    public List<StudentResultDto> StudentResults { get; set; } = new();

    // ── Đề xuất cải thiện cho lớp ──
    public List<string> Recommendations { get; set; } = new();

    // ── Debug Info (Chỉ xem trong F12) ──
    public object? DebugInfo { get; set; }
}

public class StudentSubmissionSummaryDto
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public decimal? TotalPoints { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class ChapterStatsSummaryDto
{
    public string ChapterName { get; set; } = null!;
    public int TotalAnswers { get; set; }
    public int CorrectAnswers { get; set; }
}

public class DifficultyStatsSummaryDto
{
    public int Difficulty { get; set; }
    public int TotalAnswers { get; set; }
    public int CorrectAnswers { get; set; }
}

public class QuestionStatsSummaryDto
{
    public int QuestionId { get; set; }
    public string QuestionContent { get; set; } = null!;
    public string ChapterName { get; set; } = null!;
    public int Difficulty { get; set; }
    public int TotalAttempts { get; set; }
    public int CorrectCount { get; set; }
}
