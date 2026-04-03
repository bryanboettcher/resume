using Microsoft.Extensions.Options;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Pipeline;

public sealed class DimensionPolicyEnricher : IQueryEnricher
{
    private readonly DimensionPolicyOptions _options;

    public DimensionPolicyEnricher(IOptions<DimensionPolicyOptions> options)
    {
        _options = options.Value;
    }

    public int Order => 20;

    public Task<ChatQuery> EnrichAsync(ChatQuery query, CancellationToken cancellationToken = default)
    {
        var enriched = query with
        {
            TopK = query.TopK == 5 ? _options.DefaultTopK : query.TopK,
            Dimensions = query.Dimensions ?? _options.DefaultDimensions
        };

        return Task.FromResult(enriched);
    }
}
