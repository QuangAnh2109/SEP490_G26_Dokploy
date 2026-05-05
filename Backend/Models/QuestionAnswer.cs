using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class QuestionAnswer
{
    public int QuestionAnswerId { get; set; }

    public int QuestionId { get; set; }

    public string Content { get; set; } = null!;

    public string CorrectAnswer { get; set; } = null!;

    public int? GroupAnswerId { get; set; }

    public bool? IsCorrect { get; set; }

    public int? Point { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual ICollection<BlankInput> BlankInputs { get; set; } = new List<BlankInput>();

    public virtual GroupAnswer? GroupAnswer { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}
