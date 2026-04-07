using ResumeChat.Storage.Entities;

namespace ResumeChat.Storage.Repositories;

public interface ICorpusRepository
{
    Task<IReadOnlyList<CorpusDocumentEntity>> GetAllDocumentsAsync(CancellationToken ct = default);
    Task<CorpusDocumentEntity?> GetDocumentByIdAsync(long id, CancellationToken ct = default);
    Task<CorpusDocumentEntity?> GetDocumentByPathAsync(string sourcePath, CancellationToken ct = default);
    Task UpsertDocumentAsync(CorpusDocumentEntity document, IReadOnlyList<CorpusChunkEntity> chunks, CancellationToken ct = default);
    Task<int> GetDocumentCountAsync(CancellationToken ct = default);
    Task<int> GetChunkCountAsync(CancellationToken ct = default);
}
