using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ResumeChat.Rag.Orchestration;
using ResumeChat.Storage.Options;
using ResumeChat.Storage.Orchestration;
using ResumeChat.Storage.Repositories;
using ResumeChat.Storage.Services;

namespace ResumeChat.Storage.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResumeChatStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PostgresOptions>()
            .BindConfiguration(PostgresOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CacheOptions>()
            .BindConfiguration(CacheOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddPooledDbContextFactory<ResumeChatDbContext>((sp, optionsBuilder) =>
        {
            var postgres = sp.GetRequiredService<IOptions<PostgresOptions>>().Value;
            optionsBuilder.UseNpgsql(postgres.ConnectionString);
        });

        services.AddTransient<IInteractionRepository, InteractionRepository>();
        services.AddTransient<ICorpusRepository, CorpusRepository>();
        services.AddTransient<CorpusSyncService>();
        services.AddTransient<IChatOrchestrator, CachingChatOrchestrator>();

        services.AddHostedService<MigrationHostedService>();

        return services;
    }
}
