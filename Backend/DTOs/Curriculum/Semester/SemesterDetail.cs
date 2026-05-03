using System;

namespace Backend.DTOs.Curriculum.Semester;

public class SemesterDetail : SemesterListItem
{
    public string? CreatedByName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedByName { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
