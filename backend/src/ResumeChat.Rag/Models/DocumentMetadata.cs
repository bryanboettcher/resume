namespace ResumeChat.Rag.Models;

public sealed record DocumentMetadata(
    string SourceFile,
    string? Title,
    IReadOnlyList<string> Tags);
