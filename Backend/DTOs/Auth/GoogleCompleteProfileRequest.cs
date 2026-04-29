namespace Backend.DTOs;

public class GoogleCompleteProfileRequest
{
    public string? IdToken { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? StudentId { get; set; }
}
