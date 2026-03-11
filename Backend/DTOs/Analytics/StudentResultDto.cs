namespace Backend.DTOs.Analytics;

public class StudentResultDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public decimal? TotalPoints { get; set; }
    public DateTime SubmittedAt { get; set; }
}
