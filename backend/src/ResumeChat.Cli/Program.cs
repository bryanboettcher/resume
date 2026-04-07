using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumeChat.Rag.Chunking;
using ResumeChat.Rag.Embedding;
using ResumeChat.Rag.Ingestion;
using ResumeChat.Rag.VectorStore;

// AppContext.BaseDirectory ensures appsettings.json is found next to the binary
// regardless of which working directory `dotnet run` is invoked from.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// Ollama embedding
builder.Services.AddOptions<OllamaEmbeddingOptions>()
    .BindConfiguration(OllamaEmbeddingOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<IEmbeddingProvider, OllamaEmbeddingProvider>();

// Qdrant
builder.Services.AddOptions<QdrantOptions>()
    .BindConfiguration(QdrantOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<IVectorStore, QdrantVectorStore>();

// Chunking + ingestion
builder.Services.AddSingleton<IChunkingStrategy, MarkdownSectionChunkingStrategy>();
builder.Services.AddTransient<IIngestionPipeline, CorpusIngestionPipeline>();
builder.Services.AddTransient<IngestionService>();

using var host = builder.Build();

var corpusDir = args.Length > 0 ? args[0] : builder.Configuration["Corpus:Directory"];
if (string.IsNullOrWhiteSpace(corpusDir) || !Directory.Exists(corpusDir))
{
    Console.Error.WriteLine("Usage: ResumeChat.Cli <corpus-directory>");
    Console.Error.WriteLine($"  Provided: '{corpusDir}' — directory does not exist.");
    return 1;
}

var ingestion = host.Services.GetRequiredService<IngestionService>();
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    await foreach (var progress in ingestion.IngestCorpusAsync(corpusDir, cts.Token))
    {
        Console.WriteLine($"[{progress.ChunksProcessed:D4}] {progress.Status}");
    }

    Console.WriteLine("Done.");
    return 0;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Cancelled.");
    return 130;
}
