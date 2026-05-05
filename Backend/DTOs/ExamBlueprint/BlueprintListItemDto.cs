namespace Backend.DTOs.ExamBlueprint
{
    public class BlueprintListItemDto
    {
        public int ExamBlueprintId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string? SubjectCode { get; set; }
        public int Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public DateTime UpdatedAtUtc { get; set; }
    }
}
