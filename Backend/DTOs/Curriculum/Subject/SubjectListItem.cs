namespace Backend.DTOs.Curriculum.Subject;

public class SubjectListItem
{
    public int SubjectId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Status { get; set; }
    public int ChapterCount { get; set; }
    public int ActiveChapterCount { get; set; }
    public int ClassCount { get; set; }
    public int ActiveClassCount { get; set; }
}
