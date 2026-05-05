using System;
using System.Text.Json.Serialization;

namespace Backend.DTOs.Analytics;

public class DifficultyAnalyticsDto
{
    public int DifficultyLevel { get; set; }

    /// <summary>
    /// Tên mức độ: Nhận biết, Thông hiểu, Vận dụng, Vận dụng cao
    /// </summary>
    public string DifficultyName { get; set; } = null!;

    public int TotalAnswers { get; set; }
    public int CorrectAnswers { get; set; }

    [JsonInclude]
    public double AccuracyRate => TotalAnswers == 0 ? 0 : Math.Round((double)CorrectAnswers / TotalAnswers * 100, 2);

    [JsonInclude]
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
