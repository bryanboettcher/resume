using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ResumeChat.Rag;

namespace ResumeChat.Api.Tests;

// A single factory is shared across the collection so the in-process server
// only starts once per test run.
[Collection("Api")]
public sealed class ChatApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ChatApiTests(ApiFactory factory) => _factory = factory;

    // ── health ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Health_Returns200_WithoutApiKey()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/chat/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── auth ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Chat_Returns401_WhenNoKeyHeader()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/chat", new { Message = "hi" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Chat_Returns401_WhenWrongKey()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        var response = await client.PostAsJsonAsync("/api/chat", new { Message = "hi" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Chat_Returns500_WhenApiKeyNotConfigured()
    {
        // Factory with no ApiKey in configuration.
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.UseSetting("ApiKey", ""));

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/chat", new { Message = "hi" });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ── streaming ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Chat_StreamsSseChunks_WhenValidKey()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiFactory.TestApiKey);

        var response = await client.PostAsJsonAsync("/api/chat", new { Message = "hello world" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();

        // Two words → two data lines, plus the terminal [DONE] frame.
        Assert.Contains("data: hello \n\n", body);
        Assert.Contains("data: world \n\n", body);
        Assert.Contains("data: [DONE]\n\n", body);
    }
}
