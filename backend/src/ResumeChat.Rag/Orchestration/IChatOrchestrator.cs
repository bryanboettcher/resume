using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Orchestration;

public interface IChatOrchestrator
{
    Task<ChatResult> ProcessChatAsync(ChatRequest request, CancellationToken ct = default);
}

public sealed record ChatResult(
    IAsyncEnumerable<string> Tokens,
    int ThreatScore,
    bool IsThreat,
    bool CacheHit);
