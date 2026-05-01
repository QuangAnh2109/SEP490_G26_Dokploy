using Backend.Common;
using Backend.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Middleware;

// Chặn mọi endpoint có [Authorize] khi user đã authenticate nhưng claim mcp=true.
// Endpoints opt-out bằng marker [AllowPasswordChange] (đổi password lần đầu, logout).
public sealed class FirstLoginPasswordMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            await next(context);
            return;
        }

        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next(context);
            return;
        }

        if (endpoint.Metadata.GetMetadata<AllowPasswordChangeAttribute>() is not null)
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var mcp = context.User.FindFirst(AuthClaims.MustChangePassword)?.Value;
        if (!string.Equals(mcp, AuthClaims.True, StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { code = ErrorCodes.AuthPasswordChangeRequired });
    }
}
