using Backend.Common;
using Backend.Constants;

namespace Backend.Middleware;

// User đã authenticate + claim mcp=true + endpoint không [AllowPasswordChange] → 403.
public sealed class FirstLoginPasswordMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.GetEndpoint()?.Metadata.GetMetadata<AllowPasswordChangeAttribute>() is null
            && string.Equals(context.User.FindFirst(AuthClaims.MustChangePassword)?.Value, AuthClaims.True))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { code = ErrorCodes.AuthPasswordChangeRequired });
            return;
        }

        await next(context);
    }
}
