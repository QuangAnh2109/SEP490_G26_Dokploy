namespace Backend.DTOs.Admin;

public class CreateUserResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}
