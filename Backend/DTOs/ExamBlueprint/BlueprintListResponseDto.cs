namespace Backend.DTOs.ExamBlueprint
{
    public class BlueprintListResponseDto
    {
        public List<BlueprintListItemDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }
}
