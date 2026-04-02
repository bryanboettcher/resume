using System.Runtime.CompilerServices;
using ResumeChat.Rag.Chunking;
using ResumeChat.Rag.Embedding;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Ingestion;

public sealed class CorpusIngestionPipeline : IIngestionPipeline
{
    private readonly IChunkingStrategy _chunker;
    private readonly IEmbeddingProvider _embedder;

    public CorpusIngestionPipeline(IChunkingStrategy chunker, IEmbeddingProvider embedder)
    {
        _chunker = chunker;
        _embedder = embedder;
    }

    public async IAsyncEnumerable<EmbeddedChunk> IngestAsync(
        string directory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var files = Directory.EnumerateFiles(directory, "*.md", SearchOption.AllDirectories).OrderBy(f => f);

        foreach (var filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            var relativePath = Path.GetRelativePath(directory, filePath);
            var metadata = BuildMetadata(content, relativePath);
            var chunks = _chunker.Chunk(content, metadata);

            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var embedding = await _embedder.EmbedAsync(chunk.Text, cancellationToken).ConfigureAwait(false);
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
