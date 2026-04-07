using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ResumeChat.Rag.Embedding;

public sealed class OllamaEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly OllamaEmbeddingOptions _options;
    private readonly ILogger<OllamaEmbeddingProvider> _logger;

    public OllamaEmbeddingProvider(
        HttpClient httpClient,
        IOptions<OllamaEmbeddingOptions> options,
        ILogger<OllamaEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        using var activity = RagDiagnostics.ActivitySource.StartActivity("rag.embed");
        activity?.SetTag("rag.embed.model", _options.Model);
        activity?.SetTag("rag.embed.text_length", text.Length);

        var startTimestamp = Stopwatch.GetTimestamp();

        var request = new OllamaEmbedRequest(_options.Model, text);
        var response = await _httpClient.PostAsJsonAsync(
            $"{_options.BaseUrl.TrimEnd('/')}/api/embed",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(cancellationToken);

        if (result?.Embeddings is not { Count: > 0 })
            throw new InvalidOperationException("Ollama returned no embeddings.");

        var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        var dimensions = result.Embeddings[0].Length;

        activity?.SetTag("rag.embed.dimensions", dimensions);
        RagDiagnostics.EmbeddingDuration.Record(elapsedMs);

        _logger.LogDebug("Embedded {TextLength} chars with {Model} → {Dimensions}d in {ElapsedMs:F1}ms",
            text.Length, _options.Model, dimensions, elapsedMs);

        return result.Embeddings[0];
    }

    private sealed record OllamaEmbedRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input);

    private sealed record OllamaEmbedResponse(
        [property: JsonPropertyName("embeddings")] IReadOnlyList<float[]> Embeddings);
}
