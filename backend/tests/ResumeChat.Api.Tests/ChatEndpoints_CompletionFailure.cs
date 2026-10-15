using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.Orchestration;
using ResumeChat.Rag.Response;

namespace ResumeChat.Api.Tests;

public abstract class ChatEndpoints_CompletionFailure
{
    protected WebApplicationFactory<Program> Factory = null!;
    protected HttpResponseMessage Response = null!;
    protected string Body = null!;

    [TearDown]
    public void BaseTearDown()
    {
        Response?.Dispose();
        Factory?.Dispose();
    }

    public class When_health_requested_after_completion_failed : ChatEndpoints_CompletionFailure
    {
        [SetUp]
        public async Task SetUp()
        {
            Factory = new ApiFactory();
            var client = Factory.CreateClient();
            Factory.Services.GetRequiredService<CompletionHealth>()
                .RecordFailure(new HttpRequestException("credit balance is too low"));

            Response = await client.GetAsync("/api/chat/health");
            Body = await Response.Content.ReadAsStringAsync();
        }

        [Test]
        public void It_should_return_503()
        {
            Response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        }

        [Test]
        public void It_should_report_unhealthy_status()
        {
            Body.ShouldContain("\"status\":\"unhealthy\"");
        }

        [Test]
        public void It_should_report_exception_type_as_last_error()
        {
            Body.ShouldContain("\"lastError\":\"HttpRequestException\"");
        }

        [Test]
        public void It_should_not_leak_upstream_message()
        {
            Body.ShouldNotContain("credit balance");
        }
    }

    public class When_chat_provider_fails_before_streaming : ChatEndpoints_CompletionFailure
    {
        [SetUp]
        public async Task SetUp()
        {
            Factory = new ApiFactory().WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IChatOrchestrator>();
                    services.AddSingleton<IChatOrchestrator, FailingChatOrchestrator>();
                }));
            var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", ApiFactory.TestApiKey);

            Response = await client.PostAsJsonAsync("/api/chat", new { Message = "hello" });
            Body = await Response.Content.ReadAsStringAsync();
        }

        [Test]
        public void It_should_return_503()
        {
            Response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        }

        [Test]
        public void It_should_return_problem_json()
        {
            Response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        }

        [Test]
        public void It_should_describe_provider_unavailable()
        {
            Body.ShouldContain("Completion provider unavailable");
        }
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
