using System.Collections.Generic;

namespace Backend.DTOs.Analytics;

public class ExamAnalyticsDto
{
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = null!;
    
    // Tổng số học sinh đã nộp bài (Status = 2 hoặc gì đó tương đương)
    public int TotalSubmissions { get; set; }

    // Chi tiết phân tích từng chương
    public List<ChapterAnalyticsDto> ChapterStats { get; set; } = new List<ChapterAnalyticsDto>();

    // Chuỗi các Đề xuất Actionable Insights cho Giáo viên
    public List<string> Recommendations { get; set; } = new List<string>();
}
