namespace ResumeChat.Rag.Models;

public sealed record ChatQuery
{
    public required string OriginalMessage { get; init; }
    public string ProcessedMessage { get; init; } = "";
    public int TopK { get; init; } = 5;
    public int? Dimensions { get; init; }
    public float? MinScore { get; init; }

    public static ChatQuery FromRequest(ChatRequest request) => new()
    {
        OriginalMessage = request.Message,
        ProcessedMessage = request.Message
    };
}
