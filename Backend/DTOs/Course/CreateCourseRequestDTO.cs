namespace Backend.DTOs.Course;

public class CreateCourseRequestDTO
{
    public string? ClassName { get; set; }
    public int? SubjectId { get; set; }
    public string? Semester { get; set; }
}
