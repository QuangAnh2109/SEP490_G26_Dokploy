using System;

namespace Backend.DTOs.Curriculum.Semester;

public class CreateSemesterRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
