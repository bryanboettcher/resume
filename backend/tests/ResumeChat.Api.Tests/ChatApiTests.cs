using System.Net;
using System.Net.Http.Json;

namespace ResumeChat.Api.Tests;

[TestFixture]
public sealed class ChatApiTests
{
    private ApiFactory _factory = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiFactory();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory.Dispose();
    }

    // ── health ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Health_Returns200_WithoutApiKey()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/chat/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Chat_Returns401_WhenNoKeyHeader()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/chat", new { Message = "hi" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Chat_Returns401_WhenWrongKey()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        var response = await client.PostAsJsonAsync("/api/chat", new { Message = "hi" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public void Chat_FailsStartup_WhenApiKeyNotConfigured()
    {
        // ValidateOnStart causes the host to fail during build when ApiKey is missing.
        Should.Throw<Exception>(() =>
        {
            using var noKeyFactory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.UseSetting("ApiKey:Key", ""));

            _ = noKeyFactory.CreateClient();
        });
    }

    // ── streaming ───────────────────────────────────────────────────────────

    [Test]
    public async Task Chat_StreamsSseChunks_WhenValidKey()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiFactory.TestApiKey);

        var response = await client.PostAsJsonAsync("/api/chat", new { Message = "hello world" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        var body = await response.Content.ReadAsStringAsync();

        body.ShouldContain("data: hello \n\n");
        body.ShouldContain("data: world \n\n");
        body.ShouldContain("data: [DONE]\n\n");
    }
}
