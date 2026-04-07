using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeChat.Rag;
using ResumeChat.Rag.Classification;
using ResumeChat.Rag.Completion;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.Orchestration;
using ResumeChat.Rag.Pipeline;
using ResumeChat.Rag.Response;
using ResumeChat.Storage.Entities;
using ResumeChat.Storage.Options;
using ResumeChat.Storage.Repositories;

namespace ResumeChat.Storage.Orchestration;

public sealed class CachingChatOrchestrator : IChatOrchestrator
{
    private readonly IThreatClassifier _classifier;
    private readonly IQueryTransformer _transformer;
    private readonly IResponseProvider _responseProvider;
    private readonly IInteractionRepository _interactions;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<CachingChatOrchestrator> _logger;

    public CachingChatOrchestrator(
        IThreatClassifier classifier,
        IQueryTransformer transformer,
        IResponseProvider responseProvider,
        IInteractionRepository interactions,
        IOptions<CacheOptions> cacheOptions,
        ILogger<CachingChatOrchestrator> logger)
    {
        _classifier = classifier;
        _transformer = transformer;
        _responseProvider = responseProvider;
        _interactions = interactions;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
    }

    public async Task<ChatResult> ProcessChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        var totalTimer = Stopwatch.StartNew();

        var threat = await _classifier.ClassifyAsync(request.Message, ct);
        if (threat.IsThreat)
        {
            _logger.LogWarning("Threat detected (score {Score}) for query hash {Hash}",
                threat.ThreatScore, QueryHasher.Compute(request.Message, request.History));

            await _interactions.LogInteractionAsync(new InteractionEntity
            {
                OriginalQuery = request.Message,
                ProcessedQuery = request.Message,
                ResponseText = string.Empty,
                RetrievedDocuments = "[]",
                Provider = "None",
                ModelName = "None",
                QueryHash = QueryHasher.Compute(request.Message, request.History),
                IsThreat = true,
                ThreatScore = threat.ThreatScore,
                TotalMs = totalTimer.Elapsed.TotalMilliseconds
            }, ct);

            return new ChatResult(SingleChunk(ChatResponses.Unrelated, ct), threat.ThreatScore, true, false);
        }

        var queryHash = QueryHasher.Compute(request.Message, request.History);

        if (_cacheOptions.Enabled)
        {
            var cached = await _interactions.FindCachedResponseAsync(queryHash, ct);
            if (cached is not null)
            {
                _logger.LogDebug("Cache hit for query hash {Hash}", queryHash);

                await _interactions.LogInteractionAsync(new InteractionEntity
                {
                    OriginalQuery = request.Message,
                    ProcessedQuery = cached.ProcessedQuery,
                    ResponseText = cached.ResponseText,
                    RetrievedDocuments = cached.RetrievedDocuments,
                    Provider = cached.Provider,
                    ModelName = cached.ModelName,
                    QueryHash = queryHash,
                    CacheHit = true,
                    TotalMs = totalTimer.Elapsed.TotalMilliseconds
                }, ct);

                return new ChatResult(SingleChunk(cached.ResponseText, ct), 0, false, true);
            }
        }

        var payload = await _transformer.TransformAsync(request, ct);

        var (providerName, modelName) = _responseProvider is ICompletionMetadata meta
            ? (meta.Provider, meta.Model)
            : ("Unknown", "Unknown");

        var tokens = StreamAndLog(payload, request.Message, queryHash, providerName, modelName, totalTimer, ct);
        return new ChatResult(tokens, 0, false, false);
    }

    private async IAsyncEnumerable<string> StreamAndLog(
        QueryPayload payload,
        string originalMessage,
        string queryHash,
        string providerName,
        string modelName,
        Stopwatch totalTimer,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var completionTimer = Stopwatch.StartNew();
        var responseBuilder = new StringBuilder();

        await foreach (var token in _responseProvider.GetResponseAsync(payload, ct))
        {
            responseBuilder.Append(token);
            yield return token;
        }

        completionTimer.Stop();
        totalTimer.Stop();

        var retrievedDocsJson = JsonSerializer.Serialize(
            payload.Documents.Select(d => new
            {
                source_file = d.Chunk.Metadata.SourceFile,
                section = d.Chunk.SectionHeading,
                score = d.Score
            }));

        var expiresAt = _cacheOptions.Enabled
            ? DateTimeOffset.UtcNow.AddMinutes(_cacheOptions.TtlMinutes)
            : (DateTimeOffset?)null;

        await _interactions.LogInteractionAsync(new InteractionEntity
        {
            OriginalQuery = originalMessage,
            ProcessedQuery = payload.ProcessedMessage,
            ResponseText = responseBuilder.ToString(),
            RetrievedDocuments = retrievedDocsJson,
            CompletionMs = completionTimer.Elapsed.TotalMilliseconds,
            TotalMs = totalTimer.Elapsed.TotalMilliseconds,
            Provider = providerName,
            ModelName = modelName,
            QueryHash = queryHash,
            ExpiresAt = expiresAt
        }, ct);
    }

    private static async IAsyncEnumerable<string> SingleChunk(
        string value,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.CompletedTask;
        ct.ThrowIfCancellationRequested();
        yield return value;
    }
}
