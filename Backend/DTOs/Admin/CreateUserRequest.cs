namespace Backend.DTOs.Admin;

public class CreateUserRequest
{
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public int? RoleId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? StudentId { get; set; }
}
