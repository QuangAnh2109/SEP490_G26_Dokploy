using Backend.Constants;
using Backend.Exceptions; // TODO: remove in Phase 15
using System.Net;

namespace Backend.Middleware;

public sealed class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request aborted by client at {Path}", ctx.Request.Path);
        }
        catch (BaseException be) // TODO: remove in Phase 15
        {
            logger.LogWarning(be, "Domain exception {Type} at {Path}", be.GetType().Name, ctx.Request.Path);
            ctx.Response.StatusCode = (int)be.StatusCode;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { code = be.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception at {Path} (TraceId={TraceId})",
                ctx.Request.Path, ctx.TraceIdentifier);
            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { code = ErrorCodes.Unexpected });
        }
    }
}
