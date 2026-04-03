namespace ResumeChat.Rag.Models;

public sealed record RetrievalRequest(
    string Query,
    int TopK = 5,
    int? Dimensions = null,
    float? MinScore = null);
