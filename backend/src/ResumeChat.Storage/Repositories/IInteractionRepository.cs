using ResumeChat.Storage.Entities;

namespace ResumeChat.Storage.Repositories;

public interface IInteractionRepository
{
    Task<InteractionEntity?> FindCachedResponseAsync(string queryHash, CancellationToken ct = default);
    Task LogInteractionAsync(InteractionEntity interaction, CancellationToken ct = default);
    Task<IReadOnlyList<InteractionEntity>> GetRecentAsync(int limit = 20, CancellationToken ct = default);
    Task<InteractionEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<InteractionEntity>> SearchAsync(string query, int limit = 20, CancellationToken ct = default);
    Task<bool> PurgeAsync(long id, CancellationToken ct = default);
    Task<bool> ExpireAsync(long id, CancellationToken ct = default);
}
