using Microsoft.Extensions.Options;
using ResumeChat.Api.Options;

namespace ResumeChat.Api.Middleware;

public sealed class ApiKeyMiddleware(
    RequestDelegate next,
    IOptions<ApiKeyOptions> options,
    ILogger<ApiKeyMiddleware> logger)
{
    private readonly string _configuredKey = options.Value.Key;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (context.Request.Path.Equals("/api/chat/health"))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var providedKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (providedKey != _configuredKey)
        {
            logger.LogWarning("API key auth failed for {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}
