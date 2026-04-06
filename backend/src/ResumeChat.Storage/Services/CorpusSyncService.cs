using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using ResumeChat.Rag.Chunking;
using ResumeChat.Rag.Models;
using ResumeChat.Storage.Entities;
using ResumeChat.Storage.Repositories;

namespace ResumeChat.Storage.Services;

public sealed record SyncProgress(string Status, string SourceFile, bool Skipped, int ChunkCount);

public sealed class CorpusSyncService
{
    private readonly ICorpusRepository _repository;
    private readonly IChunkingStrategy _chunker;
    private readonly ILogger<CorpusSyncService> _logger;

    public CorpusSyncService(
        ICorpusRepository repository,
        IChunkingStrategy chunker,
        ILogger<CorpusSyncService> logger)
    {
        _repository = repository;
        _chunker = chunker;
        _logger = logger;
    }

    public async IAsyncEnumerable<SyncProgress> SyncAsync(
        string directory,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var files = Directory.EnumerateFiles(directory, "*.md", SearchOption.AllDirectories).OrderBy(f => f).ToList();

        _logger.LogInformation("Starting corpus sync: {FileCount} markdown files in {Directory}", files.Count, directory);

        var synced = 0;
        var skipped = 0;

        foreach (var filePath in files)
        {
            ct.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(directory, filePath);
            var content = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var contentHash = ComputeContentHash(content);

            var existing = await _repository.GetDocumentByPathAsync(relativePath, ct).ConfigureAwait(false);
            if (existing is not null && existing.ContentHash == contentHash)
            {
                skipped++;
                yield return new SyncProgress("skipped", relativePath, Skipped: true, ChunkCount: existing.Chunks.Count);
                continue;
            }

            var title = ExtractTitle(content);
            var tags = ExtractTags(content);
            var metadata = new DocumentMetadata(relativePath, title, tags);
            var chunks = _chunker.Chunk(content, metadata);

            var document = new CorpusDocumentEntity
            {
                SourceFile = relativePath,
                Title = title,
                ContentText = content,
                ContentHash = contentHash,
                Tags = [.. tags],
                LastModified = DateTimeOffset.UtcNow
            };

            var chunkEntities = chunks
                .Select(c => new CorpusChunkEntity
                {
                    ChunkIndex = c.ChunkIndex,
                    SectionHeading = c.SectionHeading ?? string.Empty,
                    ChunkText = c.Text
                })
                .ToList();

            await _repository.UpsertDocumentAsync(document, chunkEntities, ct).ConfigureAwait(false);

            synced++;
            _logger.LogDebug("Synced {FilePath}: {ChunkCount} chunks", relativePath, chunkEntities.Count);
            yield return new SyncProgress("synced", relativePath, Skipped: false, ChunkCount: chunkEntities.Count);
        }

        _logger.LogInformation("Corpus sync complete: {Synced} synced, {Skipped} skipped", synced, skipped);
    }

    public async Task<SyncProgress> UpsertDocumentAsync(
        string sourcePath,
        string content,
        CancellationToken ct = default)
    {
        var contentHash = ComputeContentHash(content);

        var existing = await _repository.GetDocumentByPathAsync(sourcePath, ct).ConfigureAwait(false);
        if (existing is not null && existing.ContentHash == contentHash)
            return new SyncProgress("skipped", sourcePath, Skipped: true, ChunkCount: existing.Chunks.Count);

        var title = ExtractTitle(content);
        var tags = ExtractTags(content);
        var metadata = new DocumentMetadata(sourcePath, title, tags);
        var chunks = _chunker.Chunk(content, metadata);

        var document = new CorpusDocumentEntity
        {
            SourceFile = sourcePath,
            Title = title,
            ContentText = content,
            ContentHash = contentHash,
            Tags = [.. tags],
            LastModified = DateTimeOffset.UtcNow
        };

        var chunkEntities = chunks
            .Select(c => new CorpusChunkEntity
            {
                ChunkIndex = c.ChunkIndex,
                SectionHeading = c.SectionHeading ?? string.Empty,
                ChunkText = c.Text
            })
            .ToList();

        await _repository.UpsertDocumentAsync(document, chunkEntities, ct).ConfigureAwait(false);

        _logger.LogDebug("Upserted {FilePath}: {ChunkCount} chunks", sourcePath, chunkEntities.Count);
        return new SyncProgress("synced", sourcePath, Skipped: false, ChunkCount: chunkEntities.Count);
    }

    private static string ComputeContentHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
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
            if (tagsPart.StartsWith("[") && tagsPart.EndsWith("]"))
                tagsPart = tagsPart[1..^1];

            return tagsPart.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        return [];
    }
}
