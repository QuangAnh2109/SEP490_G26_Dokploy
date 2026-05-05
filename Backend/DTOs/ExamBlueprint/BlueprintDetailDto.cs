namespace Backend.DTOs.ExamBlueprint
{
    public class BlueprintDetailDto
    {
        public int ExamBlueprintId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TotalQuestions { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string? SubjectCode { get; set; }
        public int Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public DateTime UpdatedAtUtc { get; set; }
        public List<BlueprintDetailRowDto> Rows { get; set; } = new();
    }
}
