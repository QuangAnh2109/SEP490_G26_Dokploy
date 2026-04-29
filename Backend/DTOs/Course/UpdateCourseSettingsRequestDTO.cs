namespace Backend.DTOs.Course;

public class UpdateCourseSettingsRequestDTO
{
    public string? ClassName { get; set; }
    public int? InvitationCodeStatus { get; set; } // 1 for enabled, 0 for disabled
}
