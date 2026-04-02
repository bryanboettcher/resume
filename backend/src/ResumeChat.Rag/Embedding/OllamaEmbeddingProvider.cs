using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ResumeChat.Rag.Embedding;

public sealed class OllamaEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly OllamaEmbeddingOptions _options;

    public OllamaEmbeddingProvider(HttpClient httpClient, IOptions<OllamaEmbeddingOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var request = new OllamaEmbedRequest(_options.Model, text);
        var response = await _httpClient.PostAsJsonAsync(
            $"{_options.BaseUrl.TrimEnd('/')}/api/embed",
            request,
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(cancellationToken)
            .ConfigureAwait(false);

        if (result?.Embeddings is not { Count: > 0 })
            throw new InvalidOperationException("Ollama returned no embeddings.");

        return result.Embeddings[0];
    }

    private sealed record OllamaEmbedRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input);

    private sealed record OllamaEmbedResponse(
        [property: JsonPropertyName("embeddings")] IReadOnlyList<float[]> Embeddings);
}
