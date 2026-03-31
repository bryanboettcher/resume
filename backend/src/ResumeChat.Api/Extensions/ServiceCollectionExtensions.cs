using Microsoft.AspNetCore.RateLimiting;
using ResumeChat.Api.Options;

namespace ResumeChat.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResumeChatRateLimiting(
        this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>()
                      ?? new RateLimitOptions();

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiterOptions.AddFixedWindowLimiter("chat", limiter =>
            {
                limiter.PermitLimit = options.PermitLimit;
                limiter.Window = TimeSpan.FromSeconds(options.WindowSeconds);
                limiter.QueueLimit = 0;
            });
        });

        return services;
    }
}
