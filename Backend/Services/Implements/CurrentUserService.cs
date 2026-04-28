using System.Globalization;
using System.Security.Claims;
using Backend.Services.Interfaces;

namespace Backend.Services.Implements;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public int? UserId =>
        int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public int? Role =>
        int.TryParse(Principal?.FindFirstValue(ClaimTypes.Role),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out var r)
            ? r : null;
}
