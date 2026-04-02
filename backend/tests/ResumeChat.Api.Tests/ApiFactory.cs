using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ResumeChat.Rag;
using ResumeChat.Rag.Classification;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.Retrieval;

namespace ResumeChat.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string TestApiKey = "test-api-key-12345";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ApiKey:Key", TestApiKey);
        builder.UseSetting("Corpus:Directory", "/tmp/test-corpus");
        builder.UseSetting("Security:Canary", "test-canary-sentinel-value");

        builder.ConfigureServices(services =>
        {
            ReplaceService<ICompletionProvider, InstantCompletionProvider>(services);
            ReplaceService<IRetrievalProvider, EmptyRetrievalProvider>(services);
            ReplaceService<IThreatClassifier, PassthroughThreatClassifier>(services);
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

internal sealed class InstantCompletionProvider : ICompletionProvider
{
    public async IAsyncEnumerable<string> CompleteAsync(
        CompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var word in request.UserMessage.Split(' '))
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
        string query, int topK = 5, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ScoredChunk>>([]);
}

[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<ApiFactory> { }
