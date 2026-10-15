using System.ComponentModel.DataAnnotations;

namespace ResumeChat.Rag.Response;

public sealed class OpenAiResponseOptions
{
    public const string SectionName = "OpenAI";

    [Required, MinLength(1)]
    public string BaseUrl { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Model { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public int MaxTokens { get; set; } = 1024;
}
