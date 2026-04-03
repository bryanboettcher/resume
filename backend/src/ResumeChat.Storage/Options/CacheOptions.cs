namespace ResumeChat.Storage.Options;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public bool Enabled { get; set; } = true;
    public int TtlMinutes { get; set; } = 1440; // 24 hours
}
