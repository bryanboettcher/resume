using System.ComponentModel.DataAnnotations;

namespace ResumeChat.Rag.Response;

public sealed class ClaudeCliResponseOptions
{
    public const string SectionName = "ClaudeCli";

    [Required, MinLength(1)]
    public string Model { get; set; } = "sonnet";

    public string ClaudePath { get; set; } = "claude";
}
