namespace Backend.DTOs.Curriculum.Chapter;

public class CreateChapterRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
}
