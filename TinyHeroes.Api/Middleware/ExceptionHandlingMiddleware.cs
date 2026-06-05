using System.Text.Json;
using TinyHeroes.Application.Exceptions;

namespace TinyHeroes.Api.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, message) = ex switch
            {
                NotFoundException e => (StatusCodes.Status404NotFound, e.Message),
                ForbiddenException e => (StatusCodes.Status403Forbidden, e.Message),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            logger.LogError(ex,
                "Exception {ExceptionType} {Method} {Path} {RequestId} StatusCode={StatusCode}",
                ex.GetType().Name,
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier,
                statusCode);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
        }
    }
}
