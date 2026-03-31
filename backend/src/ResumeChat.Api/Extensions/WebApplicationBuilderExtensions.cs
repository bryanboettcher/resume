using ResumeChat.Api.Options;
using ResumeChat.Rag;

namespace ResumeChat.Api.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<ApiKeyOptions>()
            .BindConfiguration(ApiKeyOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddResumeChatRateLimiting(builder.Configuration);

        builder.Services.AddSingleton<ICompletionProvider, HardcodedCompletionProvider>();

        return builder;
    }
}
