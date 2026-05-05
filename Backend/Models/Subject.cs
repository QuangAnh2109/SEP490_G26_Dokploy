using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Subject
{
    public int SubjectId { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public string? Description { get; set; }
    public int Status { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int UpdatedByUserId { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;
    public virtual User UpdatedByUser { get; set; } = null!;

    public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<ExamBlueprint> ExamBlueprints { get; set; } = new List<ExamBlueprint>();

    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();
}
