namespace Backend.DTOs.Curriculum.Subject;

public class UpdateSubjectRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string ConcurrencyStamp { get; set; } = "";
}
