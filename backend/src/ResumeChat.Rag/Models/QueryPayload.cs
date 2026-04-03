namespace ResumeChat.Rag.Models;

public sealed record QueryPayload
{
    public required string OriginalMessage { get; init; }
    public required string ProcessedMessage { get; init; }
    public required IReadOnlyList<ScoredChunk> Documents { get; init; }
    public bool IsThreat { get; init; }
    public int ThreatScore { get; init; }

    public static QueryPayload Suspicious(string originalMessage, int threatScore) => new()
    {
        OriginalMessage = originalMessage,
        ProcessedMessage = originalMessage,
        Documents = [],
        IsThreat = true,
        ThreatScore = threatScore
    };

    public static QueryPayload FromQuery(ChatQuery query, IReadOnlyList<ScoredChunk> documents) => new()
    {
        OriginalMessage = query.OriginalMessage,
        ProcessedMessage = query.ProcessedMessage,
        Documents = documents,
        IsThreat = false,
        ThreatScore = 0
    };
}
