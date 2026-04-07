namespace ResumeChat.Rag.Pipeline;

public sealed class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    public int? Dimensions { get; set; }
    public int TopK { get; set; } = 5;
}
