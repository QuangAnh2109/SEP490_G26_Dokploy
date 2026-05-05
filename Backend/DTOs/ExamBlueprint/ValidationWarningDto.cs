namespace Backend.DTOs.ExamBlueprint
{
    public class ValidationWarningDto
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? ChapterId { get; set; }
        public int? Difficulty { get; set; }
    }
}
