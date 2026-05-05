using Backend.Constants;
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
        catch (Microsoft.AspNetCore.Http.BadHttpRequestException)
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { code = ErrorCodes.BadRequest });
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
