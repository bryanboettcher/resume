using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ResumeChat.Rag;

namespace ResumeChat.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string TestApiKey = "test-api-key-12345";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ApiKey:Key", TestApiKey);

        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(ICompletionProvider));
            services.Remove(descriptor);
            services.AddSingleton<ICompletionProvider, InstantCompletionProvider>();
        });
    }
}

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
