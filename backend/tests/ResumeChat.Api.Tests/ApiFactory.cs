using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ResumeChat.Rag;

namespace ResumeChat.Api.Tests;

/// <summary>
/// Shared test server configured with a known API key and a fast completion provider.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string TestApiKey = "test-api-key-12345";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ApiKey", TestApiKey);

        builder.ConfigureServices(services =>
        {
            // Replace the real provider with one that completes immediately,
            // keeping tests fast without the 50 ms per-word delay.
            var descriptor = services.Single(d => d.ServiceType == typeof(ICompletionProvider));
            services.Remove(descriptor);
            services.AddSingleton<ICompletionProvider, InstantCompletionProvider>();
        });
    }
}

/// <summary>
/// Zero-delay completion provider for test isolation.
/// </summary>
internal sealed class InstantCompletionProvider : ICompletionProvider
{
    public async IAsyncEnumerable<string> CompleteAsync(
        string prompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var word in prompt.Split(' '))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return word + " ";
        }
    }
}

[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<ApiFactory> { }
