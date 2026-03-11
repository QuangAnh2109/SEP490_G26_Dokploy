using System.Collections.Generic;

namespace Backend.DTOs.Analytics;

/// <summary>
/// DTO phân tích bài làm cá nhân — dành cho Học sinh.
/// ShowScore / ShowAnswer quyết định dữ liệu nào được trả về.
/// AnswerReview (xem lại bài làm) luôn có.
/// </summary>
public class StudentSubmissionAnalyticsDto
{
    // ── Cờ hiển thị — Frontend dựa vào đây để ẩn/hiện ──
    public bool ShowScore { get; set; }
    public bool ShowAnswer { get; set; }

    // ── Thông tin bài thi ──
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = null!;
    public int SubmissionId { get; set; }

    // ── Kết quả tổng quan (chỉ có khi ShowScore = true) ──
    public decimal? TotalPoints { get; set; }
    public int? CorrectCount { get; set; }
    public int? WrongCount { get; set; }
    public int TotalQuestions { get; set; }

    // ── So sánh với lớp (chỉ có khi ShowScore = true) ──
    public decimal? ClassAverageScore { get; set; }
    public decimal? ClassMaxScore { get; set; }

    // ── Phân tích theo chương (chỉ có khi ShowScore = true) ──
    public List<ChapterAnalyticsDto>? ChapterStats { get; set; }

    // ── Phân tích theo độ khó (chỉ có khi ShowScore = true) ──
    public List<DifficultyAnalyticsDto>? DifficultyStats { get; set; }

    // ── Đề xuất ôn tập (chỉ có khi ShowScore = true) ──
    public List<string>? Recommendations { get; set; }

    // ── Xem lại bài làm — LUÔN CÓ ──
    public List<AnswerReviewDto> AnswerReview { get; set; } = new();
}
