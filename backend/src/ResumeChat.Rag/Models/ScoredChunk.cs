namespace ResumeChat.Rag.Models;

public sealed record ScoredChunk(
    DocumentChunk Chunk,
    float Score);
