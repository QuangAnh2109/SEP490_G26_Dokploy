namespace Backend.DTOs;

public class RegisterRequest
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? StudentId { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public int? RoleId { get; set; }
}
