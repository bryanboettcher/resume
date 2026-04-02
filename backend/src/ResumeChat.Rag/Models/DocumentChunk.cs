namespace ResumeChat.Rag.Models;

public sealed record DocumentChunk(
    string Text,
    string SectionHeading,
    int ChunkIndex,
    DocumentMetadata Metadata);
