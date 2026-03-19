namespace Backend.DTOs.ExamBlueprint
{
    public class BlueprintStatusUpdateDto
    {
        public List<int> ExamBlueprintIds { get; set; } = new();
        public int Status { get; set; }
    }
}
