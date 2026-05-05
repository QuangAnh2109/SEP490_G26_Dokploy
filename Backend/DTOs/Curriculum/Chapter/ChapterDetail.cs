using System;

namespace Backend.DTOs.Curriculum.Chapter;

public class ChapterDetail : ChapterListItem
{
    public string? CreatedByName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string ConcurrencyStamp { get; set; } = "";
}
