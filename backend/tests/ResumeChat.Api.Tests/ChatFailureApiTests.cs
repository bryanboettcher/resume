using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.Orchestration;
using ResumeChat.Rag.Response;

namespace ResumeChat.Api.Tests;

[TestFixture]
public sealed class ChatFailureApiTests
{
    [Test]
    public async Task Health_Returns503Unhealthy_WhenLatestCompletionFailed()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        factory.Services.GetRequiredService<CompletionHealth>()
            .RecordFailure(new HttpRequestException("credit balance is too low"));

        var response = await client.GetAsync("/api/chat/health");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("\"status\":\"unhealthy\"");
        body.ShouldContain("\"lastError\":\"HttpRequestException\"");
        body.ShouldNotContain("credit balance");
    }

    [Test]
    public async Task Chat_Returns503Problem_WhenCompletionProviderFailsBeforeStreaming()
    {
        using var factory = new ApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IChatOrchestrator>();
                services.AddSingleton<IChatOrchestrator, FailingChatOrchestrator>();
            }));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiFactory.TestApiKey);

        var response = await client.PostAsJsonAsync("/api/chat", new { Message = "hello" });

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadAsStringAsync()).ShouldContain("Completion provider unavailable");
    }

    private sealed class FailingChatOrchestrator : IChatOrchestrator
    {
        public Task<ChatResult> ProcessChatAsync(ChatRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ChatResult(Fail(ct), ThreatScore: 0, IsThreat: false, CacheHit: false));

        private static async IAsyncEnumerable<string> Fail([EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Yield();
            throw new CompletionUnavailableException("Claude", "claude-sonnet-4-6",
                new HttpRequestException("credit balance is too low"));
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }
    }
}
