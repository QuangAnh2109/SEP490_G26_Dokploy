namespace Backend.DTOs.Class;

public class ClassDTO
{
    public int ClassId { get; set; }

    public string ClassName { get; set; } = null!;
    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = null!;
    public string? SubjectCode { get; set; }

    public string TeacherName { get; set; } = null!;

    public string InvitationCode { get; set; } = null!;
    public int InvitationCodeStatus { get; set; }

    public int SemesterId { get; set; }
    public string? SemesterCode { get; set; }

    public int StudentCount { get; set; }

    public int ExamCount { get; set; }
    public int Status { get; set; }

    public string Role { get; set; } = null!;
    public List<ChapterDTO> Chapters { get; set; } = new();
}
