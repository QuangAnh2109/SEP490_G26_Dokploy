using System.Collections.Generic;

namespace Backend.DTOs.Curriculum.Chapter;

public class ReorderChaptersRequest
{
    public List<ReorderItem> Items { get; set; } = new();

    public class ReorderItem
    {
        public int ChapterId { get; set; }
        public int DisplayOrder { get; set; }
    }
}
