namespace Backend.DTOs.ExamBlueprint;

public class BlueprintStatusUpdateDto
{
    public List<int>? ExamBlueprintIds { get; set; }
    public int? Status { get; set; }
}
