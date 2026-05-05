namespace Backend.DTOs.Analytics;

public class QuestionStatsSummaryDto
{
    public int QuestionId { get; set; }
    public string QuestionContent { get; set; } = null!;
    public string ChapterName { get; set; } = null!;
    public int Difficulty { get; set; }
    public int TotalAttempts { get; set; }
    public int CorrectCount { get; set; }
}
