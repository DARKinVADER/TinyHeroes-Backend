using System.Text.Json;
using TinyHeroes.Application.Exceptions;

namespace TinyHeroes.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next)
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

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
        }
    }
}
