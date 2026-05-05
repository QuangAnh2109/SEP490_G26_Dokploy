namespace Backend.DTOs.Class;

public class UpdateClassSettingsRequestDTO
{
    public string? ClassName { get; set; }
    public int? InvitationCodeStatus { get; set; } // 1 for enabled, 0 for disabled
}
