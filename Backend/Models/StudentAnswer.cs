using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class StudentAnswer
{
    public int StudentAnswerId { get; set; }

    public int SubmissionId { get; set; }

    public int QuestionAnswerId { get; set; }

    public string? Response { get; set; }

    public bool? IsCorrect { get; set; }

    public decimal? PointsEarned { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual QuestionAnswer QuestionAnswer { get; set; } = null!;

    public virtual Submission Submission { get; set; } = null!;
}
