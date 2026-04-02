using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Ingestion;

public interface IIngestionPipeline
{
    IAsyncEnumerable<EmbeddedChunk> IngestAsync(string directory, CancellationToken cancellationToken = default);
}
