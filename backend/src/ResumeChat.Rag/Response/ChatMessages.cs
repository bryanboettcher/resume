using System.Text.Json.Serialization;
using ResumeChat.Rag.Completion;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Response;

public sealed record ChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

public static class ChatMessages
{
    public static List<ChatMessage> Build(QueryPayload payload, string canary)
    {
        var messages = new List<ChatMessage> { new("system", SystemPromptBuilder.Build(payload, canary)) };
        if (payload.History is { Count: > 0 })
        {
            foreach (var exchange in payload.History)
            {
                messages.Add(new("user", exchange.Prompt));
                messages.Add(new("assistant", exchange.Response));
            }
        }
        messages.Add(new("user", payload.OriginalMessage));
        return messages;
    }
}
