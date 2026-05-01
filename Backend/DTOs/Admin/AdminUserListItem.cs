namespace Backend.DTOs.Admin;

public class AdminUserListItem
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int Status { get; set; }
    public bool MustChangePassword { get; set; }
    public string? PhoneNumber { get; set; }
    public string? StudentId { get; set; }
}
