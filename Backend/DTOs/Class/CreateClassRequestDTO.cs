namespace Backend.DTOs.Class;

public class CreateClassRequestDTO
{
    public string? ClassName { get; set; }
    public int? SubjectId { get; set; }
    public int? SemesterId { get; set; }
}
