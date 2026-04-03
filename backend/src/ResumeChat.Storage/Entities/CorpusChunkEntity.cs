namespace ResumeChat.Storage.Entities;

public sealed class CorpusChunkEntity
{
    public long Id { get; set; }
    public long DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public required string SectionHeading { get; set; }
    public required string ChunkText { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public CorpusDocumentEntity Document { get; set; } = null!;
}
