namespace Backend.DTOs.Curriculum.Chapter;

public class UpdateChapterRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
    public string ConcurrencyStamp { get; set; } = "";
}
