using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using ResumeChat.Rag.Embedding;
using ResumeChat.Rag.Ingestion;
using ResumeChat.Rag.Models;
using ResumeChat.Storage.Repositories;

namespace ResumeChat.Storage.Services;

public sealed class DatabaseIngestionPipeline : IIngestionPipeline
{
    private readonly ICorpusRepository _repository;
    private readonly IEmbeddingProvider _embedder;
    private readonly ILogger<DatabaseIngestionPipeline> _logger;

    public DatabaseIngestionPipeline(
        ICorpusRepository repository,
        IEmbeddingProvider embedder,
        ILogger<DatabaseIngestionPipeline> logger)
    {
        _repository = repository;
        _embedder = embedder;
        _logger = logger;
    }

    public async IAsyncEnumerable<EmbeddedChunk> IngestAsync(
        string directory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // directory parameter unused — source of truth is PG corpus table
        var documents = await _repository.GetAllDocumentsAsync(cancellationToken);

        _logger.LogInformation("DatabaseIngestionPipeline: embedding {DocumentCount} documents from PG", documents.Count);

        var totalChunks = 0;

        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadata = new DocumentMetadata(
                document.SourceFile,
                document.Title,
                document.Tags);

            foreach (var chunkEntity in document.Chunks.OrderBy(c => c.ChunkIndex))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var documentChunk = new DocumentChunk(
                    chunkEntity.ChunkText,
                    chunkEntity.SectionHeading,
                    chunkEntity.ChunkIndex,
                    metadata);

                var embedding = await _embedder.EmbedAsync(chunkEntity.ChunkText, cancellationToken);

                totalChunks++;
                yield return new EmbeddedChunk(documentChunk, embedding);
            }

            _logger.LogDebug("Embedded {ChunkCount} chunks for {SourceFile}", document.Chunks.Count, document.SourceFile);
        }

        _logger.LogInformation("DatabaseIngestionPipeline complete: {TotalChunks} chunks embedded", totalChunks);
    }
}
