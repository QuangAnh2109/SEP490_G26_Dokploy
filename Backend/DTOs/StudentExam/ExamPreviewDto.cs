namespace Backend.DTOs.StudentExam
{
    public class ExamPreviewDto
    {
        public int ExamId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int Duration { get; set; }
        public DateTime? OpenAt { get; set; }
        public DateTime? CloseAt { get; set; }
        public string Status { get; set; } = string.Empty;

        public string TeacherName { get; set; } = string.Empty;
        public DateTime UpdatedAtUtc { get; set; }
        public string? Description { get; set; }
        public string SubjectName { get; set; } = string.Empty;

        public int MaxAttempts { get; set; }
        public int RemainingAttempts { get; set; }
        public bool ShowScore { get; set; }
        public bool ShowAnswer { get; set; }
        public int PaperCount { get; set; }

        public List<BlueprintRowDto> BlueprintMatrix { get; set; } = new();
    }

    public class BlueprintRowDto
    {
        public string ChapterName { get; set; } = string.Empty;
        public int Recognize { get; set; }
        public int Understand { get; set; }
        public int Apply { get; set; }
        public int AdvancedApply { get; set; }
        public int Total { get; set; }
    }
}
