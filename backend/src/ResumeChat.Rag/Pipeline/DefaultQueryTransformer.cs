using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.Retrieval;

namespace ResumeChat.Rag.Pipeline;

public sealed class DefaultQueryTransformer : IQueryTransformer
{
    private readonly IEnumerable<IQueryEnricher> _enrichers;
    private readonly IRetrievalProvider _retrieval;
    private readonly RetrievalOptions _retrievalOptions;
    private readonly ILogger<DefaultQueryTransformer> _logger;

    public DefaultQueryTransformer(
        IEnumerable<IQueryEnricher> enrichers,
        IRetrievalProvider retrieval,
        IOptions<RetrievalOptions> retrievalOptions,
        ILogger<DefaultQueryTransformer> logger)
    {
        _enrichers = enrichers;
        _retrieval = retrieval;
        _retrievalOptions = retrievalOptions.Value;
        _logger = logger;
    }

    public async Task<QueryPayload> TransformAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = RagDiagnostics.ActivitySource.StartActivity("rag.transform");

        var query = ChatQuery.FromRequest(request);

        // Run enrichers in order
        foreach (var enricher in _enrichers.OrderBy(e => e.Order))
        {
            query = await enricher.EnrichAsync(query, cancellationToken);
        }

        activity?.SetTag("rag.transform.original", query.OriginalMessage);
        activity?.SetTag("rag.transform.processed", query.ProcessedMessage);

        // Build retrieval request from enriched query + config
        var retrievalRequest = new RetrievalRequest(
            query.ProcessedMessage,
            _retrievalOptions.TopK,
            _retrievalOptions.Dimensions,
            query.MinScore);

        activity?.SetTag("rag.transform.top_k", retrievalRequest.TopK);
        if (retrievalRequest.Dimensions.HasValue)
            activity?.SetTag("rag.transform.dimensions", retrievalRequest.Dimensions.Value);

        var documents = await _retrieval.RetrieveAsync(retrievalRequest, cancellationToken);

        _logger.LogInformation("Transformed query: {OriginalMessage} → {ProcessedMessage}, retrieved {DocumentCount} documents",
            query.OriginalMessage, query.ProcessedMessage, documents.Count);

        return QueryPayload.FromQuery(query, documents, request.History);
    }
}
