using System.ComponentModel.DataAnnotations;

namespace ResumeChat.Api.Options;

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKey";

    [Required]
    [MinLength(1)]
    public string Key { get; set; } = string.Empty;
}
