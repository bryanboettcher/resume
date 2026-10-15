using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ResumeChat.Rag.Completion;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.Response;

namespace ResumeChat.Rag.Tests;

public abstract class OpenAiResponseProvider_GetResponseAsync
{
    protected const string Canary = "canary-0123456789abcdef";

    protected string SseBody = Sse("[DONE]");
    protected string ApiKey = "";
    protected HttpStatusCode Status = HttpStatusCode.OK;
    protected string Message = "hi";
    protected IReadOnlyList<ChatExchange>? History;
    protected CompletionHealth Health = null!;
    protected RecordingHandler Handler = null!;
    protected OpenAiResponseProvider Subject = null!;
    protected List<string> Chunks = null!;

    [SetUp]
    public async Task BaseSetUp()
    {
        Health = new CompletionHealth(TimeProvider.System);

        Arrange();

        (Subject, Handler) = Create(SseBody, ApiKey, Status, Health);

        await Act();
    }

    protected virtual void Arrange() { }

    protected virtual async Task Act()
    {
        Chunks = await CollectAsync(Subject.GetResponseAsync(Payload(Message, History)));
    }

    protected static QueryPayload Payload(string message, IReadOnlyList<ChatExchange>? history = null) => new()
    {
        OriginalMessage = message,
        ProcessedMessage = message,
        Documents = [],
        History = history
    };

    protected static string Sse(params string[] dataLines) =>
        string.Concat(dataLines.Select(d => $"data: {d}\n\n"));

    protected static string ContentChunk(string content) =>
        JsonSerializer.Serialize(new { choices = new[] { new { delta = new { content } } } });

    protected static string ReasoningChunk(string reasoning) =>
        JsonSerializer.Serialize(new { choices = new[] { new { delta = new { reasoning } } } });

    private static (OpenAiResponseProvider Provider, RecordingHandler Handler) Create(
        string sseBody,
        string apiKey,
        HttpStatusCode status,
        CompletionHealth health)
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
            new HttpClient(handler), options, security, health,
            NullLogger<OpenAiResponseProvider>.Instance);
        return (provider, handler);
    }

    protected static async Task<List<string>> CollectAsync(IAsyncEnumerable<string> source)
    {
        var result = new List<string>();
        await foreach (var item in source)
            result.Add(item);
        return result;
    }

    public class When_stream_contains_reasoning_and_content : OpenAiResponseProvider_GetResponseAsync
    {
        protected override void Arrange()
        {
            Message = "languages?";
            SseBody = Sse(
                JsonSerializer.Serialize(new { choices = new[] { new { delta = new { role = "assistant", content = "" } } } }),
                ReasoningChunk("thinking about"),
                ReasoningChunk(" the answer"),
                ContentChunk("Bryan knows"),
                ContentChunk(" C#."),
                "[DONE]");
        }

        [Test]
        public void It_should_yield_only_content_deltas()
        {
            Chunks.ShouldBe(["Bryan knows", " C#."]);
        }
    }

    public class When_content_starts_with_whitespace : OpenAiResponseProvider_GetResponseAsync
    {
        protected override void Arrange()
        {
            SseBody = Sse(
                ContentChunk("\n\n"),
                ContentChunk("\nHello"),
                ContentChunk(" there\n"),
                "[DONE]");
        }

        [Test]
        public void It_should_trim_leading_whitespace_before_first_visible_token()
        {
            Chunks.ShouldBe(["Hello", " there\n"]);
        }
    }

    public class When_content_follows_done_sentinel : OpenAiResponseProvider_GetResponseAsync
    {
        protected override void Arrange()
        {
            SseBody = Sse(ContentChunk("one"), "[DONE]", ContentChunk("never"));
        }

        [Test]
        public void It_should_stop_at_done_sentinel()
        {
            Chunks.ShouldBe(["one"]);
        }
    }

    public class When_history_is_provided : OpenAiResponseProvider_GetResponseAsync
    {
        private JsonElement _root;
        private List<(string? Role, string? Content)> _messages = null!;

        protected override void Arrange()
        {
            Message = "current q";
            History = new List<ChatExchange> { new("earlier q", "earlier a") };
        }

        [SetUp]
        public void ParseRequest()
        {
            using var body = JsonDocument.Parse(Handler.RequestBody!);
            _root = body.RootElement.Clone();
            _messages = _root.GetProperty("messages").EnumerateArray()
                .Select(m => (m.GetProperty("role").GetString(), m.GetProperty("content").GetString()))
                .ToList();
        }

        [Test]
        public void It_should_post_to_chat_completions()
        {
            Handler.RequestUri.ShouldBe(new Uri("https://llm.example/v1/chat/completions"));
        }

        [Test]
        public void It_should_send_configured_model()
        {
            _root.GetProperty("model").GetString().ShouldBe("qwen3.8");
        }

        [Test]
        public void It_should_request_streaming()
        {
            _root.GetProperty("stream").GetBoolean().ShouldBeTrue();
        }

        [Test]
        public void It_should_send_configured_max_tokens()
        {
            _root.GetProperty("max_tokens").GetInt32().ShouldBe(512);
        }

        [Test]
        public void It_should_send_system_then_history_then_current_roles()
        {
            _messages.Select(m => m.Role).ShouldBe(["system", "user", "assistant", "user"]);
        }

        [Test]
        public void It_should_include_canary_in_system_prompt()
        {
            _messages[0].Content!.ShouldContain(Canary);
        }

        [Test]
        public void It_should_send_history_question()
        {
            _messages[1].Content.ShouldBe("earlier q");
        }

        [Test]
        public void It_should_send_history_answer()
        {
            _messages[2].Content.ShouldBe("earlier a");
        }

        [Test]
        public void It_should_send_current_question_last()
        {
            _messages[3].Content.ShouldBe("current q");
        }
    }

    public class When_api_key_configured : OpenAiResponseProvider_GetResponseAsync
    {
        protected override void Arrange()
        {
            ApiKey = "sk-test";
        }

        [Test]
        public void It_should_use_bearer_scheme()
        {
            Handler.Authorization?.Scheme.ShouldBe("Bearer");
        }

        [Test]
        public void It_should_send_api_key_as_token()
        {
            Handler.Authorization?.Parameter.ShouldBe("sk-test");
        }
    }

    public class When_api_key_not_configured : OpenAiResponseProvider_GetResponseAsync
    {
        [Test]
        public void It_should_not_send_authorization()
        {
            Handler.Authorization.ShouldBeNull();
        }
    }

    public class When_upstream_returns_error_status : OpenAiResponseProvider_GetResponseAsync
    {
        private CompletionUnavailableException _exception = null!;

        protected override void Arrange()
        {
            SseBody = "{\"error\":\"nope\"}";
            Status = HttpStatusCode.BadRequest;
        }

        protected override async Task Act()
        {
            _exception = await Should.ThrowAsync<CompletionUnavailableException>(
                () => CollectAsync(Subject.GetResponseAsync(Payload(Message, History))));
        }

        [Test]
        public void It_should_report_provider()
        {
            _exception.Provider.ShouldBe("OpenAI");
        }

        [Test]
        public void It_should_report_model()
        {
            _exception.Model.ShouldBe("qwen3.8");
        }

        [Test]
        public void It_should_wrap_http_request_exception()
        {
            _exception.InnerException.ShouldBeOfType<HttpRequestException>();
        }

        [Test]
        public void It_should_record_unhealthy()
        {
            Health.Snapshot().Healthy.ShouldBeFalse();
        }

        [Test]
        public void It_should_record_exception_type_as_last_error()
        {
            Health.Snapshot().LastError.ShouldBe(nameof(HttpRequestException));
        }
    }

    public class When_stream_completes : OpenAiResponseProvider_GetResponseAsync
    {
        protected override void Arrange()
        {
            SseBody = Sse(ContentChunk("ok"), "[DONE]");
        }

        [Test]
        public void It_should_record_healthy()
        {
            Health.Snapshot().Healthy.ShouldBeTrue();
        }

        [Test]
        public void It_should_record_last_success()
        {
            Health.Snapshot().LastSuccess.ShouldNotBeNull();
        }

        [Test]
        public void It_should_not_record_last_failure()
        {
            Health.Snapshot().LastFailure.ShouldBeNull();
        }
    }

    protected sealed class RecordingHandler(string responseBody, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
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
