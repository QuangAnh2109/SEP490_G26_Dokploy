using System;
using System.Text.Json.Serialization;

namespace Backend.DTOs.Analytics;

public class HardestQuestionDto
{
    public int QuestionId { get; set; }
    public string QuestionContent { get; set; } = null!;
    public string ChapterName { get; set; } = null!;
    public int Difficulty { get; set; }
    public string DifficultyName { get; set; } = null!;
    public int TotalAttempts { get; set; }
    public int CorrectCount { get; set; }
    [JsonInclude]
    public double AccuracyRate => TotalAttempts == 0 ? 0 : Math.Round((double)CorrectCount / TotalAttempts * 100, 2);
}
