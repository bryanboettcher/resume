using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Completion;

public sealed class OllamaCompletionProvider : CompletionProviderBase
{
    private readonly HttpClient _httpClient;
    private readonly OllamaCompletionOptions _options;

    public OllamaCompletionProvider(
        HttpClient httpClient,
        IOptions<OllamaCompletionOptions> options,
        IOptions<CompletionSecurityOptions> security,
        ILogger<OllamaCompletionProvider> logger)
        : base(security.Value, logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    protected override string ProviderName => "Ollama";
    protected override string ModelName => _options.Model;

    protected override async IAsyncEnumerable<string> StreamTokensAsync(
        string systemPrompt,
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var activity = RagDiagnostics.ActivitySource.StartActivity("rag.complete");
        activity?.SetTag("rag.complete.model", _options.Model);
        activity?.SetTag("rag.complete.provider", ProviderName);
        activity?.SetTag("rag.complete.context_chunks", request.Context.Count);

        var totalStart = Stopwatch.GetTimestamp();
        var firstTokenRecorded = false;

        Logger.LogInformation("Starting Ollama completion with model {Model} ({ContextChunks} context chunks)",
            _options.Model, request.Context.Count);

        var body = new
        {
            model = _options.Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = request.UserMessage }
            },
            stream = true
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/api/chat")
        {
            Content = JsonContent.Create(body)
        };

        using var response = await _httpClient.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var chunk = JsonSerializer.Deserialize<OllamaChatChunk>(line);
            if (chunk?.Message?.Content is { Length: > 0 } content)
            {
                if (!firstTokenRecorded)
                {
                    RagDiagnostics.CompletionFirstTokenDuration.Record(
                        Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds);
                    firstTokenRecorded = true;
                }

                yield return content;
            }

            if (chunk?.Done == true)
                break;
        }

        var totalMs = Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds;
        RagDiagnostics.CompletionTotalDuration.Record(totalMs);
        Logger.LogInformation("Ollama completion finished in {ElapsedMs:F1}ms", totalMs);
    }

    private sealed record OllamaChatChunk(
        [property: JsonPropertyName("message")] OllamaChatMessage? Message,
        [property: JsonPropertyName("done")] bool Done);

    private sealed record OllamaChatMessage(
        [property: JsonPropertyName("content")] string Content);
}
