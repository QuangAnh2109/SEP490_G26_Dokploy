namespace Backend.DTOs.Profile;

public class UserProfileDTO
{
    public int UserId { get; set; }

    public string Email { get; set; } = null!;

    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? StudentId { get; set; }

    public int RoleId { get; set; }

    public int Status { get; set; }
}
