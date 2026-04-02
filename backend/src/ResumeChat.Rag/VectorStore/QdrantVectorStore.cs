using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.VectorStore;

public sealed class QdrantVectorStore : IVectorStore
{
    private readonly HttpClient _httpClient;
    private readonly QdrantOptions _options;

    public QdrantVectorStore(HttpClient httpClient, IOptions<QdrantOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task EnsureCollectionAsync(int vectorSize, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/collections/{_options.CollectionName}";

        var check = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (check.IsSuccessStatusCode)
            return;

        var body = new
        {
            vectors = new
            {
                size = vectorSize,
                distance = "Cosine"
            }
        };

        var response = await _httpClient.PutAsJsonAsync(url, body, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpsertAsync(EmbeddedChunk chunk, CancellationToken cancellationToken = default)
    {
        var pointId = GeneratePointId(chunk.Chunk);

        var point = new
        {
            points = new[]
            {
                new
                {
                    id = pointId,
                    vector = chunk.Embedding.ToArray(),
                    payload = new Dictionary<string, object>
                    {
                        ["text"] = chunk.Chunk.Text,
                        ["section_heading"] = chunk.Chunk.SectionHeading,
                        ["chunk_index"] = chunk.Chunk.ChunkIndex,
                        ["source_file"] = chunk.Chunk.Metadata.SourceFile,
                        ["title"] = chunk.Chunk.Metadata.Title ?? "",
                        ["tags"] = chunk.Chunk.Metadata.Tags
                    }
                }
            }
        };

        var url = $"{BaseUrl}/collections/{_options.CollectionName}/points";
        var response = await _httpClient.PutAsJsonAsync(url, point, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<ScoredChunk>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            vector = queryEmbedding.ToArray(),
            limit = topK,
            with_payload = true
        };

        var url = $"{BaseUrl}/collections/{_options.CollectionName}/points/search";
        var response = await _httpClient.PostAsJsonAsync(url, body, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(cancellationToken)
            .ConfigureAwait(false);

        if (result?.Result is null)
            return [];

        return result.Result.Select(r =>
        {
            var payload = r.Payload;
            var metadata = new DocumentMetadata(
                GetString(payload, "source_file"),
                GetStringOrNull(payload, "title"),
                payload.TryGetValue("tags", out var tagsEl) ? DeserializeTags(tagsEl) : []);

            var chunk = new DocumentChunk(
                GetString(payload, "text"),
                GetString(payload, "section_heading"),
                payload.TryGetValue("chunk_index", out var idx) && idx.TryGetInt32(out var i) ? i : 0,
                metadata);

            return new ScoredChunk(chunk, r.Score);
        }).ToList();
    }

    private string BaseUrl => _options.BaseUrl.TrimEnd('/');

    private static string GeneratePointId(DocumentChunk chunk)
    {
        var input = $"{chunk.Metadata.SourceFile}:{chunk.SectionHeading}:{chunk.ChunkIndex}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        // Qdrant accepts UUID-format string IDs — use first 16 bytes as a deterministic UUID
        return new Guid(hash.AsSpan(0, 16)).ToString();
    }

    private static string GetString(Dictionary<string, JsonElement> payload, string key) =>
        payload.TryGetValue(key, out var el) ? el.GetString() ?? "" : "";

    private static string? GetStringOrNull(Dictionary<string, JsonElement> payload, string key) =>
        payload.TryGetValue(key, out var el) ? el.GetString() : null;

    private static IReadOnlyList<string> DeserializeTags(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
            return [];

        return element.EnumerateArray()
            .Select(e => e.GetString() ?? "")
            .ToList();
    }

    private sealed record QdrantSearchResponse(
        [property: JsonPropertyName("result")] IReadOnlyList<QdrantSearchResult> Result);

    private sealed record QdrantSearchResult(
        [property: JsonPropertyName("score")] float Score,
        [property: JsonPropertyName("payload")] Dictionary<string, JsonElement> Payload);
}
