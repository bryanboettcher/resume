using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ResumeChat.Rag;

public static class RagDiagnostics
{
    public const string ServiceName = "ResumeChat";
    public const string ActivitySourceName = "ResumeChat.Rag";
    public const string MeterName = "ResumeChat.Rag";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    // Counters
    public static readonly Counter<long> ChatRequests =
        Meter.CreateCounter<long>("resumechat.chat.requests", "requests", "Total chat requests");

    public static readonly Counter<long> ChatErrors =
        Meter.CreateCounter<long>("resumechat.chat.errors", "errors", "Chat request errors");

    public static readonly Counter<long> IngestionChunks =
        Meter.CreateCounter<long>("resumechat.ingestion.chunks", "chunks", "Chunks ingested");

    // Histograms
    public static readonly Histogram<double> EmbeddingDuration =
        Meter.CreateHistogram<double>("resumechat.rag.embedding.duration", "ms", "Embedding latency");

    public static readonly Histogram<double> VectorSearchDuration =
        Meter.CreateHistogram<double>("resumechat.rag.vector_search.duration", "ms", "Vector search latency");

    public static readonly Histogram<double> RetrievalDuration =
        Meter.CreateHistogram<double>("resumechat.rag.retrieval.duration", "ms", "Full retrieval latency");

    public static readonly Histogram<double> CompletionFirstTokenDuration =
        Meter.CreateHistogram<double>("resumechat.rag.completion.first_token.duration", "ms", "Completion time to first token");

    public static readonly Histogram<double> CompletionTotalDuration =
        Meter.CreateHistogram<double>("resumechat.rag.completion.total_duration", "ms", "Total completion streaming duration");

    public static readonly Histogram<int> RetrievalResultCount =
        Meter.CreateHistogram<int>("resumechat.rag.retrieval.result_count", "chunks", "Chunks returned per retrieval");

    public static readonly Histogram<double> TopRetrievalScore =
        Meter.CreateHistogram<double>("resumechat.rag.retrieval.top_score", "score", "Highest similarity score per query");

    // Gauges
    public static readonly UpDownCounter<int> IngestionInProgress =
        Meter.CreateUpDownCounter<int>("resumechat.ingestion.in_progress", "operations", "Active ingestion operations");
}
