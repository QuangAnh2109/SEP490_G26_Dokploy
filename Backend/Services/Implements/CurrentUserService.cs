using System.Globalization;
using System.Security.Claims;
using Backend.Services.Interfaces;

namespace Backend.Services.Implements;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User
        ?? throw new InvalidOperationException("CurrentUserService accessed outside HTTP context.");

    public int UserId =>
        int.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : throw new InvalidOperationException("UserId claim missing or invalid.");

    public string Email =>
        Principal.FindFirstValue(ClaimTypes.Email)
            ?? throw new InvalidOperationException("Email claim missing.");

    public string Role =>
        Principal.FindFirstValue(ClaimTypes.Role)
            ?? throw new InvalidOperationException("Role claim missing.");
}
