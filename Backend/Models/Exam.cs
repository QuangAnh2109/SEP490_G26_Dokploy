using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Exam
{
    public int ExamId { get; set; }

    public int? ExamBlueprintId { get; set; }

    public int TeacherId { get; set; }

    public int? ClassId { get; set; }

    public string Title { get; set; } = null!;

    public int SubjectId { get; set; }

    public string? Description { get; set; }

    public int Duration { get; set; }

    public bool ShowScore { get; set; }

    public bool ShowAnswer { get; set; }

    public int MaxAttempts { get; set; }

    public DateTime? VisibleFrom { get; set; }

    public DateTime? OpenAt { get; set; }

    public DateTime? CloseAt { get; set; }

    public bool ShuffleQuestion { get; set; }

    public bool AllowLateSubmission { get; set; }

    public int Status { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual Class? Class { get; set; }

    public virtual ExamBlueprint? ExamBlueprint { get; set; }

    public virtual ICollection<Paper> Papers { get; set; } = new List<Paper>();

    public virtual Subject Subject { get; set; } = null!;

    public virtual User Teacher { get; set; } = null!;
}
