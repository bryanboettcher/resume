using Microsoft.EntityFrameworkCore;
using ResumeChat.Storage.Entities;

namespace ResumeChat.Storage.Repositories;

internal sealed class CorpusRepository(IDbContextFactory<ResumeChatDbContext> contextFactory) : ICorpusRepository
{
    public async Task<IReadOnlyList<CorpusDocumentEntity>> GetAllDocumentsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        return await context.CorpusDocuments
            .Include(d => d.Chunks)
            .OrderBy(d => d.SourceFile)
            .ToListAsync(ct);
    }

    public async Task<CorpusDocumentEntity?> GetDocumentByIdAsync(long id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        return await context.CorpusDocuments
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<CorpusDocumentEntity?> GetDocumentByPathAsync(string sourcePath, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        return await context.CorpusDocuments
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.SourceFile == sourcePath, ct);
    }

    public async Task UpsertDocumentAsync(CorpusDocumentEntity document, IReadOnlyList<CorpusChunkEntity> chunks, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        var existing = await context.CorpusDocuments
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.SourceFile == document.SourceFile, ct);

        if (existing is not null)
        {
            existing.Title = document.Title;
            existing.ContentText = document.ContentText;
            existing.ContentHash = document.ContentHash;
            existing.Tags = document.Tags;
            existing.LastModified = document.LastModified;

            context.CorpusChunks.RemoveRange(existing.Chunks);
            await context.SaveChangesAsync(ct);

            foreach (var chunk in chunks)
                chunk.DocumentId = existing.Id;

            context.CorpusChunks.AddRange(chunks);
        }
        else
        {
            document.Chunks.Clear();
            context.CorpusDocuments.Add(document);
            await context.SaveChangesAsync(ct);

            foreach (var chunk in chunks)
                chunk.DocumentId = document.Id;

            context.CorpusChunks.AddRange(chunks);
        }

        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<int> GetDocumentCountAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.CorpusDocuments.CountAsync(ct);
    }

    public async Task<int> GetChunkCountAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.CorpusChunks.CountAsync(ct);
    }
}
