namespace Backend.DTOs.ExamBlueprint
{
    public class ChapterOptionDto
    {
        public int ChapterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ChapterAvailabilityDto> AvailabilityByDifficulty { get; set; } = new();
    }
}
