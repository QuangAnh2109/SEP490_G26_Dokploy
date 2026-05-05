namespace Backend.DTOs;

public class GoogleLoginRequest
{
    public string? IdToken { get; set; }
    public bool RememberMe { get; set; }
}
