using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResumeChat.Rag.Classification;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.Orchestration;
using ResumeChat.Rag.Pipeline;
using ResumeChat.Rag.Response;
using ResumeChat.Rag.Retrieval;
using ResumeChat.Storage;
using ResumeChat.Storage.Entities;
using ResumeChat.Storage.Repositories;

namespace ResumeChat.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string TestApiKey = "test-api-key-12345";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ApiKey:Key", TestApiKey);
        builder.UseSetting("Corpus:Directory", "/tmp/test-corpus");
        builder.UseSetting("Security:Canary", "test-canary-sentinel-value");
        // Supply a syntactically valid connection string so the options validation passes,
        // but we remove the DbContextFactory registration below so no real connection is made.
        builder.UseSetting("Postgres:ConnectionString", "Host=localhost;Database=test");

        builder.ConfigureServices(services =>
        {
            ReplaceService<IResponseProvider, InstantResponseProvider>(services);
            ReplaceService<IRetrievalProvider, EmptyRetrievalProvider>(services);
            ReplaceService<IThreatClassifier, PassthroughThreatClassifier>(services);
            ReplaceService<IChatOrchestrator, PassthroughChatOrchestrator>(services);
            ReplaceService<IInteractionRepository, NullInteractionRepository>(services);
            ReplaceService<ICorpusRepository, EmptyCorpusRepository>(services);

            // Remove the pooled DbContextFactory — no real PG in tests
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbContextFactory<ResumeChatDbContext>));
            if (dbContextDescriptor is not null)
                services.Remove(dbContextDescriptor);

            // Also remove the migration hosted service which depends on the factory
            var migrationDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(MigrationHostedService));
            if (migrationDescriptor is not null)
                services.Remove(migrationDescriptor);
        });
    }

    private static void ReplaceService<TService, TImpl>(IServiceCollection services)
        where TService : class
        where TImpl : class, TService
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TService));
        if (descriptor is not null)
            services.Remove(descriptor);
        services.AddSingleton<TService, TImpl>();
    }
}

internal sealed class InstantResponseProvider : IResponseProvider
{
    public async IAsyncEnumerable<string> GetResponseAsync(
        QueryPayload payload,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var word in payload.OriginalMessage.Split(' '))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return word + " ";
        }
    }
}

internal sealed class EmptyRetrievalProvider : IRetrievalProvider
{
    public Task<IReadOnlyList<ScoredChunk>> RetrieveAsync(
        RetrievalRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ScoredChunk>>([]);
}

internal sealed class PassthroughChatOrchestrator : IChatOrchestrator
{
    public Task<ChatResult> ProcessChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(new ChatResult(
            EchoTokens(request.Message, ct),
            ThreatScore: 0,
            IsThreat: false,
            CacheHit: false));
    }

    private static async IAsyncEnumerable<string> EchoTokens(
        string message,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var word in message.Split(' '))
        {
            ct.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return word + " ";
        }
    }
}

internal sealed class NullInteractionRepository : IInteractionRepository
{
    public Task<InteractionEntity?> FindCachedResponseAsync(string queryHash, CancellationToken ct = default) =>
        Task.FromResult<InteractionEntity?>(null);

    public Task LogInteractionAsync(InteractionEntity interaction, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<InteractionEntity>> GetRecentAsync(int limit = 20, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<InteractionEntity>>([]);

    public Task<InteractionEntity?> GetByIdAsync(long id, CancellationToken ct = default) =>
        Task.FromResult<InteractionEntity?>(null);

    public Task<IReadOnlyList<InteractionEntity>> SearchAsync(string query, int limit = 20, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<InteractionEntity>>([]);

    public Task<bool> PurgeAsync(long id, CancellationToken ct = default) =>
        Task.FromResult(false);

    public Task<bool> ExpireAsync(long id, CancellationToken ct = default) =>
        Task.FromResult(false);
}

internal sealed class EmptyCorpusRepository : ICorpusRepository
{
    public Task<IReadOnlyList<CorpusDocumentEntity>> GetAllDocumentsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CorpusDocumentEntity>>([]);

    public Task<CorpusDocumentEntity?> GetDocumentByPathAsync(string sourcePath, CancellationToken ct = default) =>
        Task.FromResult<CorpusDocumentEntity?>(null);

    public Task UpsertDocumentAsync(CorpusDocumentEntity document, IReadOnlyList<CorpusChunkEntity> chunks, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<int> GetDocumentCountAsync(CancellationToken ct = default) =>
        Task.FromResult(0);

    public Task<int> GetChunkCountAsync(CancellationToken ct = default) =>
        Task.FromResult(0);
}
