using System;

namespace Backend.DTOs.Class
{
    public class ChapterDTO
    {
        public int ChapterId { get; set; }
        public int SubjectId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
