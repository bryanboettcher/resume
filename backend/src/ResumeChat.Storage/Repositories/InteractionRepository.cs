using Microsoft.EntityFrameworkCore;
using ResumeChat.Storage.Entities;

namespace ResumeChat.Storage.Repositories;

internal sealed class InteractionRepository(IDbContextFactory<ResumeChatDbContext> contextFactory) : IInteractionRepository
{
    public async Task<InteractionEntity?> FindCachedResponseAsync(string queryHash, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.Interactions
            .Where(i => i.QueryHash == queryHash && i.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task LogInteractionAsync(InteractionEntity interaction, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        context.Interactions.Add(interaction);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<InteractionEntity>> GetRecentAsync(int limit = 20, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.Interactions
            .OrderByDescending(i => i.CreatedAt)
            .Take(limit)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<InteractionEntity?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.Interactions
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<InteractionEntity>> SearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var lowerQuery = query.ToLowerInvariant();
        return await context.Interactions
            .Where(i => i.OriginalQuery.ToLower().Contains(lowerQuery)
                     || i.ResponseText.ToLower().Contains(lowerQuery))
            .OrderByDescending(i => i.CreatedAt)
            .Take(limit)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> PurgeAsync(long id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var rows = await context.Interactions
            .Where(i => i.Id == id)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        return rows > 0;
    }

    public async Task<bool> ExpireAsync(long id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var rows = await context.Interactions
            .Where(i => i.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.QueryHash, "")
                .SetProperty(i => i.ExpiresAt, DateTimeOffset.UtcNow),
            ct).ConfigureAwait(false);

        return rows > 0;
    }
}
