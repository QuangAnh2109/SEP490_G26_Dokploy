using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Question
{
    public int QuestionId { get; set; }

    public int CreatedByUserId { get; set; }

    public string QuestionType { get; set; } = null!;

    public string QuestionContent { get; set; } = null!;

    public int ChapterId { get; set; }

    public int Difficulty { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string Status { get; set; } = null!;

    public byte[] ConcurrencyStamp { get; set; } = null!;

    public byte QuestionPurpose { get; set; }

    public virtual Chapter Chapter { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<QuestionAnswer> QuestionAnswers { get; set; } = new List<QuestionAnswer>();

    public virtual ICollection<Paper> Papers { get; set; } = new List<Paper>();
}
