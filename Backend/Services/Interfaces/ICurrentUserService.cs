namespace Backend.Services.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Email { get; }
    int? Role { get; }
}
