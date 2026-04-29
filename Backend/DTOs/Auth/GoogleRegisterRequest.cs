namespace Backend.DTOs;

public class GoogleRegisterRequest
{
    public string? IdToken { get; set; }
    public int? RoleId { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? StudentId { get; set; }
}
