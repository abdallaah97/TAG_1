using Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace API.Middleware
{
    // One place that turns an exception into a response, so services can just throw.
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AppException exception)
            {
                _logger.LogWarning(exception, "Handled application error on {Path}", context.Request.Path);
                await WriteAsync(context, exception.StatusCode, exception.Message);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unhandled error on {Path}", context.Request.Path);
                await WriteAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
            }
        }

        private static async Task WriteAsync(HttpContext context, HttpStatusCode statusCode, string message)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new
            {
                status = (int)statusCode,
                message,
                traceId = context.TraceIdentifier
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
