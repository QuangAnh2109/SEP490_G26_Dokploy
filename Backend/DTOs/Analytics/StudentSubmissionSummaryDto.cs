using System;

namespace Backend.DTOs.Analytics;

public class StudentSubmissionSummaryDto
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public decimal? TotalPoints { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
