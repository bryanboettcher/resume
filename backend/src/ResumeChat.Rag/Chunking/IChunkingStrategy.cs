using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Chunking;

public interface IChunkingStrategy
{
    IReadOnlyList<DocumentChunk> Chunk(string content, DocumentMetadata metadata);
}
