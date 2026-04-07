using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ResumeChat.Api.Options;

namespace ResumeChat.Api.Middleware;

public sealed class ApiKeyMiddleware(
    RequestDelegate next,
    IOptions<ApiKeyOptions> options,
    ILogger<ApiKeyMiddleware> logger)
{
    private readonly byte[] _configuredKeyBytes = Encoding.UTF8.GetBytes(options.Value.Key);

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        if (context.Request.Path.Equals("/api/chat/health"))
        {
            await next(context);
            return;
        }

        var providedKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (!IsKeyValid(providedKey))
        {
            logger.LogWarning("API key auth failed for {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    private bool IsKeyValid(string? providedKey)
    {
        if (providedKey is null)
            return false;

        var byteCount = Encoding.UTF8.GetByteCount(providedKey);
        Span<byte> providedBytes = stackalloc byte[byteCount];
        Encoding.UTF8.GetBytes(providedKey, providedBytes);

        return CryptographicOperations.FixedTimeEquals(providedBytes, _configuredKeyBytes);
    }
}
