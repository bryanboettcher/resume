using System.ComponentModel.DataAnnotations;

namespace ResumeChat.Rag.Completion;

public sealed class OllamaCompletionOptions
{
    public const string SectionName = "Ollama:Completion";

    [Required, MinLength(1)]
    public string BaseUrl { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Model { get; set; } = "llama3.2";
}
