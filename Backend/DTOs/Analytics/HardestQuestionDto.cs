namespace Backend.DTOs.Analytics;

public class HardestQuestionDto
{
    public int QuestionId { get; set; }
    public string ContentPreview { get; set; } = null!;
    public string ChapterName { get; set; } = null!;
    public int TotalAttempts { get; set; }
    public int CorrectCount { get; set; }
    public double ErrorRate { get; set; }
}
