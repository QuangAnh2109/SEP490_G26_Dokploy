using System;

namespace Backend.DTOs.Curriculum.Semester;

public class UpdateSemesterRequest
{
    public string Name { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string ConcurrencyStamp { get; set; } = "";
}
