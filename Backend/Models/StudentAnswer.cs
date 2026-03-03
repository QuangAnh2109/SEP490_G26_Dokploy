using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class StudentAnswer
{
    public int StudentAnswersId { get; set; }

    public int SubmissionId { get; set; }

    public int QuestionAnswersId { get; set; }

    public string? Response { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual QuestionAnswer QuestionAnswers { get; set; } = null!;

    public virtual Submission Submission { get; set; } = null!;
}
