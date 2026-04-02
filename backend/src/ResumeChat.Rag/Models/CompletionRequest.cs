namespace ResumeChat.Rag.Models;

public sealed record CompletionRequest(
    string UserMessage,
    IReadOnlyList<ScoredChunk> Context);
