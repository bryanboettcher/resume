namespace ResumeChat.Rag.Models;

public record ChatRequest(string Message, IReadOnlyList<ChatExchange>? History = null);

public record ChatExchange(string Prompt, string Response);
