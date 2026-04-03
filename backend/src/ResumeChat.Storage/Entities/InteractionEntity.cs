namespace ResumeChat.Storage.Entities;

public sealed class InteractionEntity
{
    public long Id { get; set; }
    public required string OriginalQuery { get; set; }
    public required string ProcessedQuery { get; set; }
    public required string ResponseText { get; set; }
    public required string RetrievedDocuments { get; set; } // JSON: [{source_file, section, score}]
    public double? RetrievalMs { get; set; }
    public double? CompletionMs { get; set; }
    public double? TotalMs { get; set; }
    public required string Provider { get; set; }
    public required string ModelName { get; set; }
    public bool IsThreat { get; set; }
    public int ThreatScore { get; set; }
    public required string QueryHash { get; set; }
    public bool CacheHit { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
}
