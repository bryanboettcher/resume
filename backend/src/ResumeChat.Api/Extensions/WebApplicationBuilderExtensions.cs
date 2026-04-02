using FluentValidation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ResumeChat.Api.Options;
using ResumeChat.Rag;
using ResumeChat.Rag.Chunking;
using ResumeChat.Rag.Classification;
using ResumeChat.Rag.Completion;
using ResumeChat.Rag.Embedding;
using ResumeChat.Rag.Ingestion;
using ResumeChat.Rag.Retrieval;
using ResumeChat.Rag.VectorStore;

namespace ResumeChat.Api.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.ConfigureRagTelemetry();

        builder.Services.AddOptions<ApiKeyOptions>()
            .BindConfiguration(ApiKeyOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        builder.Services.AddResumeChatRateLimiting(builder.Configuration);
        builder.AddRagServices();

        return builder;
    }

    private static void ConfigureRagTelemetry(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource(RagDiagnostics.ActivitySourceName))
            .WithMetrics(metrics => metrics.AddMeter(RagDiagnostics.MeterName));
    }

    private static void AddRagServices(this WebApplicationBuilder builder)
    {
        // Embedding (Ollama)
        builder.Services.AddOptions<OllamaEmbeddingOptions>()
            .BindConfiguration(OllamaEmbeddingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddHttpClient<IEmbeddingProvider, OllamaEmbeddingProvider>();

        // Vector store (Qdrant)
        builder.Services.AddOptions<QdrantOptions>()
            .BindConfiguration(QdrantOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddHttpClient<IVectorStore, QdrantVectorStore>();

        // Corpus config
        builder.Services.AddOptions<Api.Options.CorpusOptions>()
            .BindConfiguration(Api.Options.CorpusOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Chunking
        builder.Services.AddSingleton<IChunkingStrategy, MarkdownSectionChunkingStrategy>();

        // Ingestion
        builder.Services.AddTransient<IIngestionPipeline, CorpusIngestionPipeline>();
        builder.Services.AddTransient<IngestionService>();

        // Retrieval
        builder.Services.AddTransient<IRetrievalProvider, VectorRetrievalProvider>();

        // Threat classification
        var guardProvider = builder.Configuration["Guard:Provider"];
        if (guardProvider == "Ollama")
        {
            builder.Services.AddOptions<OllamaThreatClassifierOptions>()
                .BindConfiguration(OllamaThreatClassifierOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();
            builder.Services.AddHttpClient<IThreatClassifier, OllamaThreatClassifier>();
        }
        else
        {
            builder.Services.AddSingleton<IThreatClassifier, PassthroughThreatClassifier>();
        }

        // Security (canary for prompt injection detection)
        builder.Services.AddOptions<CompletionSecurityOptions>()
            .BindConfiguration(CompletionSecurityOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Completion — select provider based on configuration
        var completionProvider = builder.Configuration["Completion:Provider"];
        switch (completionProvider)
        {
            case "Claude":
                builder.Services.AddOptions<ClaudeCompletionOptions>()
                    .BindConfiguration(ClaudeCompletionOptions.SectionName)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                builder.Services.AddHttpClient<ICompletionProvider, ClaudeCompletionProvider>();
                break;

            case "Ollama":
                builder.Services.AddOptions<OllamaCompletionOptions>()
                    .BindConfiguration(OllamaCompletionOptions.SectionName)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                builder.Services.AddHttpClient<ICompletionProvider, OllamaCompletionProvider>();
                break;

            default:
                builder.Services.AddSingleton<ICompletionProvider, HardcodedCompletionProvider>();
                break;
        }
    }
}
