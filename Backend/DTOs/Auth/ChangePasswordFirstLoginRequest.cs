namespace Backend.DTOs.Auth;

public class ChangePasswordFirstLoginRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
