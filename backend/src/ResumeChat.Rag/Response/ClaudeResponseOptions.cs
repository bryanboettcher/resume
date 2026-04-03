using System.ComponentModel.DataAnnotations;

namespace ResumeChat.Rag.Response;

public sealed class ClaudeResponseOptions
{
    public const string SectionName = "Claude";

    [Required, MinLength(1)]
    public string ApiKey { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Model { get; set; } = "claude-sonnet-4-20250514";

    public int MaxTokens { get; set; } = 1024;
}
