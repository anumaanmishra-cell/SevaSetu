using System.Net;
using System.Text.Json;
using SevaSetu.Application.Common;

namespace SevaSetu.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var status = ex is AppException app ? app.StatusCode : (int)HttpStatusCode.InternalServerError;
            if (status >= 500)
                logger.LogError(ex, "Unhandled error");
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
    }
}

public class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId => accessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    public string? Role => accessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
}
