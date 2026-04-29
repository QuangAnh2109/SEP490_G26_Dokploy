namespace Backend.Services.Interfaces;

/// <summary>
/// Truy cập user từ JWT claim. Chỉ resolve khi action có [Authorize(Roles = ...)] —
/// truy cập trong context anonymous sẽ throw InvalidOperationException.
/// </summary>
public interface ICurrentUserService
{
    int UserId { get; }
    string Email { get; }
    string Role { get; }
}
