namespace ResumeChat.Api.Options;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
}
