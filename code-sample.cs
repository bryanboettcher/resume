app.MapPost("api/chat", HandleChat);

private static async Task<IResult> HandleChat(
    ChatRequest request,
    IQueryTransformer transformer,
    IResponseProvider provider,
    ILogger<ChatRequest> logger,
    HttpListenerContext context,
    CancellationToken cancellationToken)
{
    var response = await HandleQuery();
    var chunks = await HandleResponse(response);

    return await StreamSse(chunks);
    
    async Task<QueryPayload?> HandleQuery()
        => await transformer.TransformAsync(request, cancellationToken);

    async IAsyncEnumerable<string> HandleResponse(QueryPayload? payload)
    {
        if (payload is null)
            return ChatResponses.ServerError;

        if (payload.IsThreat)
            return ChatResponses.Unrelated;

        return payload.ResponseChunks;
    }
}

public sealed record ChatRequest(string Message);

public interface IQueryTransformer
{
    Task<QueryPayload?> TransformAsync(ChatRequest request, CancellationToken cancellationToken);
}

public class DefaultQueryTransformer : IQueryTransformer
{
    public DefaultQueryTransformer(
        IThreatClassifier threatClassifier,
        IQueryEnhancer queryEnhancer,
        IRetrievalProvider retrievalProvider
    )
    {
        _threatClassifier = threatClassifier;
        _queryEnhancer = queryEnhancer;
        _retrievalProvider = retrievalProvider;
    }

    public async Task<QueryPayload?> TransformAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        if (await classifier.ClassifyAsync(request, cancellationToken))
            return QueryPayload.Suspicious;

        var expandedQuery = await _queryEnhancer.EnhanceAsync(request, cancellationToken);
        var documents = await _retrievalProvider.RetrieveAsync(expandedQuery, cancellationToken);
        
        return new QueryPayload(expandedQuery, documents);
    }
}

public interface IQueryTransformer
{
    Task<ChatQuery> TransformAsync(ChatRequest request, CancellationToken cancellationToken);
}

public class CompositeQueryTransformer : IQueryTransformer
{
    private readonly IEnumerable<IQueryEnricher> _enrichers;

    public CompositeQueryTransformer(IEnumerable<IQueryEnricher> enrichers)
        => _enrichers = enrichers;

    public async Task<ChatQuery> TransformAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var query = new ChatQuery(request);

        foreach (var enhancer in _enhancers.OrderBy(e => e.Order))
            query = await enhancer.EnhanceAsync(query, cancellationToken);

        return query;
    }
}

public interface IQueryEnricher
{
    Task<ChatQuery> EnhanceAsync(ChatQuery query, CancellationToken cancellationToken);
    int Order { get; }
}

public class HardcodedSynonymExpansionEnricher : IQueryEnricher
{
    public int Order => 10;

    public Task<ChatQuery> EnhanceAsync(ChatQuery query, CancellationToken cancellationToken)
    {
        var expandedMessage = query.Message.Replace("AI", "Artificial Intelligence");
        return Task.FromResult(new ChatQuery(expandedMessage));
    }
}

public class ProfanityFilterEnricher : IQueryEnricher
{
    public int Order => 20;

    public Task<ChatQuery> EnhanceAsync(ChatQuery query, CancellationToken cancellationToken)
    {
        var cleanedMessage = query.Message.Replace("badword", "****");
        return Task.FromResult(new ChatQuery(cleanedMessage));
    }
}

public interface IResponseProvider
{
    IAsyncEnumerable<string> GetResponseChunks(QueryPayload payload);
}

public class ClaudeResponseProvider : IResponseProvider
{
    public IAsyncEnumerable<string> GetResponseChunks(QueryPayload payload)
    {
        // normal implementation here
    }
}