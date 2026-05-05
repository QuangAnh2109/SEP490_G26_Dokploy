using System;

namespace Backend.DTOs.Curriculum.Semester;

public class SemesterListItem
{
    public int SemesterId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Status { get; set; }
    public int ClassCount { get; set; }
    public int ActiveClassCount { get; set; }
    public int ActiveExamCount { get; set; }
    public string ConcurrencyStamp { get; set; } = "";
}
