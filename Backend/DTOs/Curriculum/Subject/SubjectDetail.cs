using System;
using System.Collections.Generic;
using Backend.DTOs.Curriculum.Chapter;

namespace Backend.DTOs.Curriculum.Subject;

public class SubjectDetail : SubjectListItem
{
    public string? Description { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedByName { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string ConcurrencyStamp { get; set; } = "";
    public List<ChapterListItem> Chapters { get; set; } = new();
}
