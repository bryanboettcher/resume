using Microsoft.Extensions.Logging;
using ResumeChat.Rag.Classification;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.Retrieval;

namespace ResumeChat.Rag.Pipeline;

public sealed class DefaultQueryTransformer : IQueryTransformer
{
    private readonly IThreatClassifier _classifier;
    private readonly IEnumerable<IQueryEnricher> _enrichers;
    private readonly IRetrievalProvider _retrieval;
    private readonly ILogger<DefaultQueryTransformer> _logger;

    public DefaultQueryTransformer(
        IThreatClassifier classifier,
        IEnumerable<IQueryEnricher> enrichers,
        IRetrievalProvider retrieval,
        ILogger<DefaultQueryTransformer> logger)
    {
        _classifier = classifier;
        _enrichers = enrichers;
        _retrieval = retrieval;
        _logger = logger;
    }

    public async Task<QueryPayload?> TransformAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = RagDiagnostics.ActivitySource.StartActivity("rag.transform");

        var query = ChatQuery.FromRequest(request);

        // Threat classification on raw user input
        var threat = await _classifier.ClassifyAsync(query.OriginalMessage, cancellationToken).ConfigureAwait(false);
        if (threat.IsThreat)
        {
            _logger.LogWarning("Threat detected (score {ThreatScore}): {Message}",
                threat.ThreatScore, query.OriginalMessage);
            return QueryPayload.Suspicious(query.OriginalMessage, threat.ThreatScore);
        }

        // Run enrichers in order
        foreach (var enricher in _enrichers.OrderBy(e => e.Order))
        {
            query = await enricher.EnrichAsync(query, cancellationToken).ConfigureAwait(false);
        }

        activity?.SetTag("rag.transform.original", query.OriginalMessage);
        activity?.SetTag("rag.transform.processed", query.ProcessedMessage);
        activity?.SetTag("rag.transform.top_k", query.TopK);
        if (query.Dimensions.HasValue)
            activity?.SetTag("rag.transform.dimensions", query.Dimensions.Value);

        // Retrieve documents using enriched query
        var retrievalRequest = new RetrievalRequest(
            query.ProcessedMessage,
            query.TopK,
            query.Dimensions,
            query.MinScore);

        var documents = await _retrieval.RetrieveAsync(retrievalRequest, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Transformed query: {OriginalMessage} → {ProcessedMessage}, retrieved {DocumentCount} documents",
            query.OriginalMessage, query.ProcessedMessage, documents.Count);

        return QueryPayload.FromQuery(query, documents);
    }
}
