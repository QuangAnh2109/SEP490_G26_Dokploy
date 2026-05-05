using System;

namespace Backend.DTOs.Curriculum.Chapter;

public class ChapterListItem
{
    public int ChapterId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public int Status { get; set; }
    public int QuestionCount { get; set; }
    public string? UpdatedByName { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string ConcurrencyStamp { get; set; } = "";
}
