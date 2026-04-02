using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.VectorStore;

public sealed class QdrantVectorStore : IVectorStore
{
    private readonly HttpClient _httpClient;
    private readonly QdrantOptions _options;
    private readonly ILogger<QdrantVectorStore> _logger;

    public QdrantVectorStore(
        HttpClient httpClient,
        IOptions<QdrantOptions> options,
        ILogger<QdrantVectorStore> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureCollectionAsync(int vectorSize, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/collections/{_options.CollectionName}";

        var check = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (check.IsSuccessStatusCode)
        {
            _logger.LogDebug("Collection {Collection} already exists", _options.CollectionName);
            return;
        }

        _logger.LogInformation("Creating collection {Collection} with vector size {VectorSize}",
            _options.CollectionName, vectorSize);

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
        using var activity = RagDiagnostics.ActivitySource.StartActivity("rag.vector_search");
        activity?.SetTag("rag.search.top_k", topK);

        var startTimestamp = Stopwatch.GetTimestamp();

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
        {
            _logger.LogWarning("Qdrant search returned null result for top_k={TopK}", topK);
            RagDiagnostics.VectorSearchDuration.Record(Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
            return [];
        }

        var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        activity?.SetTag("rag.search.result_count", result.Result.Count);
        activity?.SetTag("rag.search.top_score", result.Result.Count > 0 ? result.Result[0].Score : 0);
        RagDiagnostics.VectorSearchDuration.Record(elapsedMs);

        _logger.LogDebug("Qdrant search returned {ResultCount} results in {ElapsedMs:F1}ms (top score: {TopScore:F4})",
            result.Result.Count, elapsedMs, result.Result.Count > 0 ? result.Result[0].Score : 0);

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
