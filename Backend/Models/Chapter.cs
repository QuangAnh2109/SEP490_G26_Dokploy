using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Chapter
{
    public int ChapterId { get; set; }

    public int SubjectId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public int Status { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int UpdatedByUserId { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;
    public virtual User UpdatedByUser { get; set; } = null!;

    public virtual ICollection<ExamBlueprintChapter> ExamBlueprintChapters { get; set; } = new List<ExamBlueprintChapter>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual Subject Subject { get; set; } = null!;
}
