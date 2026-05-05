namespace Backend.DTOs.ExamBlueprint;

public class CreateExamBlueprintRowDto
{
    public int? ChapterId { get; set; }
    public int? Difficulty { get; set; }
    public int? TotalQuestions { get; set; }
}
