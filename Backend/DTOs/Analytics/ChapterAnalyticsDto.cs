namespace Backend.DTOs.Analytics;

public class ChapterAnalyticsDto
{
    public int ChapterId { get; set; }
    public string ChapterName { get; set; } = null!;
    
    // Tổng số câu trả lời của tất cả học sinh cho chương này
    public int TotalAnswers { get; set; }
    
    // Số câu trả lời đúng
    public int CorrectAnswers { get; set; }
    
    // Tỉ lệ đúng (0 - 100%)
    public double AccuracyRate => TotalAnswers == 0 ? 0 : Math.Round((double)CorrectAnswers / TotalAnswers * 100, 2);
    
    // Trạng thái theo Rule (Báo động, Cần chú ý, Tốt)
    public string Status 
    {
        get
        {
            if (TotalAnswers == 0) return "Chưa đủ dữ liệu";
            if (AccuracyRate < 40) return "Báo động";
            if (AccuracyRate < 70) return "Cần chú ý";
            return "Tốt";
        }
    }
}
