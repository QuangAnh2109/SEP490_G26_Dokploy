using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Submission
{
    public int SubmissionId { get; set; }

    public int StudentId { get; set; }

    public int PaperId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public decimal? TotalPoints { get; set; }

    public int Status { get; set; }

    public byte GradingStatus { get; set; }

    public string? GradingError { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual Paper Paper { get; set; } = null!;

    public virtual User Student { get; set; } = null!;

    public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}
