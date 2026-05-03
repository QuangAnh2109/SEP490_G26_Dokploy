using System;

namespace Backend.DTOs.Class;

public class SemesterOptionDto
{
    public int SemesterId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Status { get; set; }
}
