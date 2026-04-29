namespace Backend.DTOs;

public class VerifyOtpRequest
{
    public string? Email { get; set; }
    public string? OtpCode { get; set; }
}
