using System.Net;
using System.Text.Json;
using Backend.Exceptions;

namespace Backend.Helper
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var response = new 
            {
                success = false,
                message = exception.Message,
                details = (object?)null
            };

            int statusCode = (int)HttpStatusCode.InternalServerError;

            if (exception is BaseException baseEx)
            {
                statusCode = (int)baseEx.StatusCode;
                response = new 
                {
                    success = false,
                    message = baseEx.Message,
                    details = baseEx.Details
                };
            }
            else if (exception is KeyNotFoundException)
            {
                statusCode = (int)HttpStatusCode.NotFound;
            }
            else if (exception is UnauthorizedAccessException)
            {
                statusCode = (int)HttpStatusCode.Forbidden;
            }

            context.Response.StatusCode = statusCode;

            // In development, provide more info for generic exceptions
            if (_env.IsDevelopment() && statusCode == 500 && !(exception is BaseException))
            {
                response = new 
                {
                    success = false,
                    message = exception.Message,
                    details = (object?)$"{exception.StackTrace}"
                };
            }

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);
            await context.Response.WriteAsync(json);
        }
    }
}
