namespace Backend.DTOs.StudentExam
{
    public class ExamInfoForStudentDto
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int MaxAttempts { get; set; }
        public int StudentAttempts { get; set; }
        public DateTime? CloseAt { get; set; }
        public bool ShuffleQuestion { get; set; }
        public List<int> PaperIds { get; set; } = null!;
    }
}
