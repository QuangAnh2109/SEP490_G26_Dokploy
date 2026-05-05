namespace Backend.DTOs.ExamBlueprint
{
    public class SubjectOptionDto
    {
        public int SubjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
    }
}
