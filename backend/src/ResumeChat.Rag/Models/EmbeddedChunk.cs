namespace ResumeChat.Rag.Models;

public sealed record EmbeddedChunk(
    DocumentChunk Chunk,
    ReadOnlyMemory<float> Embedding);
