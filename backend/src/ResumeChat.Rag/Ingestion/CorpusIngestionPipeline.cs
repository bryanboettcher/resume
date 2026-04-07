using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using ResumeChat.Rag.Chunking;
using ResumeChat.Rag.Embedding;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Ingestion;

public sealed class CorpusIngestionPipeline : IIngestionPipeline
{
    private readonly IChunkingStrategy _chunker;
    private readonly IEmbeddingProvider _embedder;
    private readonly ILogger<CorpusIngestionPipeline> _logger;

    public CorpusIngestionPipeline(
        IChunkingStrategy chunker,
        IEmbeddingProvider embedder,
        ILogger<CorpusIngestionPipeline> logger)
    {
        _chunker = chunker;
        _embedder = embedder;
        _logger = logger;
    }

    public async IAsyncEnumerable<EmbeddedChunk> IngestAsync(
        string directory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var files = Directory.EnumerateFiles(directory, "*.md", SearchOption.AllDirectories).OrderBy(f => f);

        foreach (var filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var fileActivity = RagDiagnostics.ActivitySource.StartActivity("rag.ingest.file");
            var relativePath = Path.GetRelativePath(directory, filePath);
            fileActivity?.SetTag("rag.ingest.file_path", relativePath);

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var metadata = BuildMetadata(content, relativePath);
            var chunks = _chunker.Chunk(content, metadata);

            fileActivity?.SetTag("rag.ingest.chunk_count", chunks.Count);
            _logger.LogDebug("Processing {FilePath}: {ChunkCount} chunks", relativePath, chunks.Count);

            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var embedding = await _embedder.EmbedAsync(chunk.Text, cancellationToken);
                yield return new EmbeddedChunk(chunk, embedding);
            }
        }
    }

    private static DocumentMetadata BuildMetadata(string content, string relativePath)
    {
        var title = ExtractTitle(content);
        var tags = ExtractTags(content);
        return new DocumentMetadata(relativePath, title, tags);
    }

    private static string? ExtractTitle(string content)
    {
        foreach (var line in content.AsSpan().EnumerateLines())
        {
            if (line.StartsWith("# ") && !line.StartsWith("## "))
                return line[2..].Trim().ToString();
        }

        return null;
    }

    private static IReadOnlyList<string> ExtractTags(string content)
    {
        foreach (var line in content.AsSpan().EnumerateLines())
        {
            if (!line.StartsWith("tags:"))
                continue;

            var tagsPart = line[5..].Trim();
            // Strip surrounding brackets: [tag1, tag2, tag3]
            if (tagsPart.StartsWith("[") && tagsPart.EndsWith("]"))
                tagsPart = tagsPart[1..^1];

            return tagsPart.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        return [];
    }
}
