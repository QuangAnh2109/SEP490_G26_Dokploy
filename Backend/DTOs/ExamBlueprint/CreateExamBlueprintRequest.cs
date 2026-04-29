namespace Backend.DTOs.ExamBlueprint;

public class CreateExamBlueprintRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? SubjectId { get; set; }
    public int? TargetTotalQuestions { get; set; }
    public int? TargetStatus { get; set; }
    public List<CreateExamBlueprintRowDto>? Rows { get; set; }
}
