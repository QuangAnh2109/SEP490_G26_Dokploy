namespace Backend.DTOs.Analytics;

public class ChapterStatsSummaryDto
{
    public string ChapterName { get; set; } = null!;
    public int TotalAnswers { get; set; }
    public int CorrectAnswers { get; set; }
}
