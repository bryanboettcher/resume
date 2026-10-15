using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ResumeChat.Rag.Completion;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.Response;

namespace ResumeChat.Rag.Tests;

[TestFixture]
public sealed class OpenAiResponseProviderTests
{
    private const string Canary = "canary-0123456789abcdef";

    private static QueryPayload Payload(string message, IReadOnlyList<ChatExchange>? history = null) => new()
    {
        OriginalMessage = message,
        ProcessedMessage = message,
        Documents = [],
        History = history
    };

    private static string Sse(params string[] dataLines) =>
        string.Concat(dataLines.Select(d => $"data: {d}\n\n"));

    private static string ContentChunk(string content) =>
        JsonSerializer.Serialize(new { choices = new[] { new { delta = new { content } } } });

    private static string ReasoningChunk(string reasoning) =>
        JsonSerializer.Serialize(new { choices = new[] { new { delta = new { reasoning } } } });

    private static (OpenAiResponseProvider Provider, RecordingHandler Handler) Create(
        string sseBody,
        string apiKey = "",
        HttpStatusCode status = HttpStatusCode.OK,
        CompletionHealth? health = null)
    {
        var handler = new RecordingHandler(sseBody, status);
        var options = Options.Create(new OpenAiResponseOptions
        {
            BaseUrl = "https://llm.example/v1/",
            Model = "qwen3.8",
            ApiKey = apiKey,
            MaxTokens = 512
        });
        var security = Options.Create(new CompletionSecurityOptions { Canary = Canary });
        var provider = new OpenAiResponseProvider(
            new HttpClient(handler), options, security, health ?? new CompletionHealth(TimeProvider.System),
            NullLogger<OpenAiResponseProvider>.Instance);
        return (provider, handler);
    }

    [Test]
    public async Task GetResponseAsync_YieldsContentDeltas_AndIgnoresReasoning()
    {
        var (provider, _) = Create(Sse(
            JsonSerializer.Serialize(new { choices = new[] { new { delta = new { role = "assistant", content = "" } } } }),
            ReasoningChunk("thinking about"),
            ReasoningChunk(" the answer"),
            ContentChunk("Bryan knows"),
            ContentChunk(" C#."),
            "[DONE]"));

        var chunks = await CollectAsync(provider.GetResponseAsync(Payload("languages?")));

        chunks.ShouldBe(["Bryan knows", " C#."]);
    }

    [Test]
    public async Task GetResponseAsync_TrimsLeadingWhitespace_BeforeFirstVisibleToken()
    {
        var (provider, _) = Create(Sse(
            ContentChunk("\n\n"),
            ContentChunk("\nHello"),
            ContentChunk(" there\n"),
            "[DONE]"));

        var chunks = await CollectAsync(provider.GetResponseAsync(Payload("hi")));

        chunks.ShouldBe(["Hello", " there\n"]);
    }

    [Test]
    public async Task GetResponseAsync_StopsAtDoneSentinel()
    {
        var (provider, _) = Create(Sse(ContentChunk("one"), "[DONE]", ContentChunk("never")));

        var chunks = await CollectAsync(provider.GetResponseAsync(Payload("hi")));

        chunks.ShouldBe(["one"]);
    }

    [Test]
    public async Task GetResponseAsync_PostsStreamingChatCompletion_WithHistoryAndCanaryPrompt()
    {
        var (provider, handler) = Create(Sse("[DONE]"));
        var history = new List<ChatExchange> { new("earlier q", "earlier a") };

        await CollectAsync(provider.GetResponseAsync(Payload("current q", history)));

        handler.RequestUri.ShouldBe(new Uri("https://llm.example/v1/chat/completions"));
        using var body = JsonDocument.Parse(handler.RequestBody!);
        var root = body.RootElement;
        root.GetProperty("model").GetString().ShouldBe("qwen3.8");
        root.GetProperty("stream").GetBoolean().ShouldBeTrue();
        root.GetProperty("max_tokens").GetInt32().ShouldBe(512);
        var messages = root.GetProperty("messages").EnumerateArray()
            .Select(m => (m.GetProperty("role").GetString(), m.GetProperty("content").GetString()))
            .ToList();
        messages.Select(m => m.Item1).ShouldBe(["system", "user", "assistant", "user"]);
        messages[0].Item2!.ShouldContain(Canary);
        messages[1].Item2.ShouldBe("earlier q");
        messages[2].Item2.ShouldBe("earlier a");
        messages[3].Item2.ShouldBe("current q");
    }

    [Test]
    public async Task GetResponseAsync_SendsBearerToken_OnlyWhenApiKeyConfigured()
    {
        var (withKey, keyedHandler) = Create(Sse("[DONE]"), apiKey: "sk-test");
        var (withoutKey, anonymousHandler) = Create(Sse("[DONE]"));

        await CollectAsync(withKey.GetResponseAsync(Payload("hi")));
        await CollectAsync(withoutKey.GetResponseAsync(Payload("hi")));

        keyedHandler.Authorization?.Scheme.ShouldBe("Bearer");
        keyedHandler.Authorization?.Parameter.ShouldBe("sk-test");
        anonymousHandler.Authorization.ShouldBeNull();
    }

    [Test]
    public async Task GetResponseAsync_OnErrorStatus_ThrowsCompletionUnavailable_AndRecordsFailure()
    {
        var health = new CompletionHealth(TimeProvider.System);
        var (provider, _) = Create("{\"error\":\"nope\"}", status: HttpStatusCode.BadRequest, health: health);

        var ex = await Should.ThrowAsync<CompletionUnavailableException>(
            () => CollectAsync(provider.GetResponseAsync(Payload("hi"))));

        ex.Provider.ShouldBe("OpenAI");
        ex.Model.ShouldBe("qwen3.8");
        ex.InnerException.ShouldBeOfType<HttpRequestException>();
        var snapshot = health.Snapshot();
        snapshot.Healthy.ShouldBeFalse();
        snapshot.LastError.ShouldBe(nameof(HttpRequestException));
    }

    [Test]
    public async Task GetResponseAsync_CompletedStream_RecordsSuccess()
    {
        var health = new CompletionHealth(TimeProvider.System);
        var (provider, _) = Create(Sse(ContentChunk("ok"), "[DONE]"), health: health);

        await CollectAsync(provider.GetResponseAsync(Payload("hi")));

        var snapshot = health.Snapshot();
        snapshot.Healthy.ShouldBeTrue();
        snapshot.LastSuccess.ShouldNotBeNull();
        snapshot.LastFailure.ShouldBeNull();
    }

    private static async Task<List<string>> CollectAsync(IAsyncEnumerable<string> source)
    {
        var result = new List<string>();
        await foreach (var item in source)
            result.Add(item);
        return result;
    }

    private sealed class RecordingHandler(string responseBody, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? RequestBody { get; private set; }
        public System.Net.Http.Headers.AuthenticationHeaderValue? Authorization { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Authorization = request.Headers.Authorization;
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "text/event-stream")
            };
        }
    }
}
