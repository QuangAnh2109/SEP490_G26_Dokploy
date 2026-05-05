namespace Backend.DTOs.Curriculum.Subject;

public class CreateSubjectRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}
