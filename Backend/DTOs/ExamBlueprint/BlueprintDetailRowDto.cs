namespace Backend.DTOs.ExamBlueprint
{
    public class BlueprintDetailRowDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = string.Empty;
        public int Difficulty { get; set; }
        public string DifficultyLabel { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
    }
}
