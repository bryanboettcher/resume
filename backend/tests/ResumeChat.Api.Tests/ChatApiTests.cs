using System.Net;
using System.Net.Http.Json;

namespace ResumeChat.Api.Tests;

[Collection("Api")]
public sealed class ChatApiTests(ApiFactory factory)
{
    // ── health ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Health_Returns200_WithoutApiKey()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/chat/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── auth ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Chat_Returns401_WhenNoKeyHeader()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/chat", new { Message = "hi" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Chat_Returns401_WhenWrongKey()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        var response = await client.PostAsJsonAsync("/api/chat", new { Message = "hi" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Chat_FailsStartup_WhenApiKeyNotConfigured()
    {
        // ValidateOnStart causes the host to fail during build when ApiKey is missing.
        Assert.ThrowsAny<Exception>(() =>
        {
            using var noKeyFactory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.UseSetting("ApiKey:Key", ""));

            _ = noKeyFactory.CreateClient();
        });
    }

    // ── streaming ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Chat_StreamsSseChunks_WhenValidKey()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiFactory.TestApiKey);

        var response = await client.PostAsJsonAsync("/api/chat", new { Message = "hello world" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("data: hello \n\n", body);
        Assert.Contains("data: world \n\n", body);
        Assert.Contains("data: [DONE]\n\n", body);
    }
}
