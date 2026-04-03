namespace ResumeChat.Storage.Entities;

public sealed class CorpusDocumentEntity
{
    public long Id { get; set; }
    public required string SourceFile { get; set; }
    public string? Title { get; set; }
    public required string ContentText { get; set; }
    public required string ContentHash { get; set; }
    public List<string> Tags { get; set; } = [];
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<CorpusChunkEntity> Chunks { get; set; } = [];
}
