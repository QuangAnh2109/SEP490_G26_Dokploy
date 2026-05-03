namespace Backend.DTOs.Curriculum;

public class CurriculumListQuery
{
    public string? Q { get; set; }
    public int? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
