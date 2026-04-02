using System.ComponentModel.DataAnnotations;

namespace ResumeChat.Rag.VectorStore;

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    [Required, MinLength(1)]
    public string BaseUrl { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string CollectionName { get; set; } = "resume-chunks";
}
