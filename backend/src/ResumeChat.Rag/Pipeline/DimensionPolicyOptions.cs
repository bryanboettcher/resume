namespace ResumeChat.Rag.Pipeline;

public sealed class DimensionPolicyOptions
{
    public const string SectionName = "DimensionPolicy";

    public int? DefaultDimensions { get; set; }
    public int DefaultTopK { get; set; } = 5;
}
