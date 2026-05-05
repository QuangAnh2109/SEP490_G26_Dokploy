namespace Backend.DTOs.ExamBlueprint
{
    public class CreateExamBlueprintResponse
    {
        public int ExamBlueprintId { get; set; }
        public int Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public DateTime UpdatedAtUtc { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ValidationWarningDto> Warnings { get; set; } = new();
    }
}
