namespace Backend.DTOs.ExamBlueprint;

public class BlueprintListQueryDto
{
    public string? Keyword { get; set; }
    public int? SubjectId { get; set; }
    public int? Page { get; set; } = 1;
    public int? PageSize { get; set; } = 10;
}
