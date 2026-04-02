using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Completion;

public sealed class ClaudeCompletionProvider : ICompletionProvider
{
    private readonly HttpClient _httpClient;
    private readonly ClaudeCompletionOptions _options;
    private readonly CompletionSecurityOptions _security;
    private readonly ILogger<ClaudeCompletionProvider> _logger;

    public ClaudeCompletionProvider(
        HttpClient httpClient,
        IOptions<ClaudeCompletionOptions> options,
        IOptions<CompletionSecurityOptions> security,
        ILogger<ClaudeCompletionProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _security = security.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> CompleteAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = RagDiagnostics.ActivitySource.StartActivity("rag.complete");
        activity?.SetTag("rag.complete.model", _options.Model);
        activity?.SetTag("rag.complete.provider", "Claude");
        activity?.SetTag("rag.complete.context_chunks", request.Context.Count);

        var totalStart = Stopwatch.GetTimestamp();
        var firstTokenRecorded = false;

        _logger.LogInformation("Starting Claude completion with model {Model} ({ContextChunks} context chunks)",
            _options.Model, request.Context.Count);

        var systemPrompt = SystemPromptBuilder.Build(request, _security.Canary);

        var body = new
        {
            model = _options.Model,
            max_tokens = _options.MaxTokens,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = request.UserMessage }
            },
            stream = true
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(body)
        };
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");

        using var response = await _httpClient.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                continue;

            var json = line["data: ".Length..];

            var evt = JsonSerializer.Deserialize<ClaudeStreamEvent>(json);
            if (evt?.Type == "content_block_delta" && evt.Delta?.Text is { Length: > 0 } text)
            {
                if (!firstTokenRecorded)
                {
                    RagDiagnostics.CompletionFirstTokenDuration.Record(
                        Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds);
                    firstTokenRecorded = true;
                }

                yield return text;
            }
            else if (evt?.Type == "message_stop")
                break;
        }

        var totalMs = Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds;
        RagDiagnostics.CompletionTotalDuration.Record(totalMs);
        _logger.LogInformation("Claude completion finished in {ElapsedMs:F1}ms", totalMs);
    }

    private sealed record ClaudeStreamEvent(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("delta")] ClaudeDelta? Delta);

    private sealed record ClaudeDelta(
        [property: JsonPropertyName("text")] string? Text);
}
