using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeChat.Corpus.Cli;

var command = args.Length > 0 ? args[0] : "scan";
var filter = AnalysisFilter.Parse(args, startIndex: 1);

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Services.AddOptions<CorpusOptions>()
    .BindConfiguration(CorpusOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<OllamaOptions>()
    .BindConfiguration(OllamaOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContextFactory<CorpusDbContext>((sp, options) =>
{
    var corpus = sp.GetRequiredService<IOptions<CorpusOptions>>().Value;
    options.UseNpgsql(corpus.ConnectionString);
});

builder.Services.AddSingleton<SourceTreeWalker>();
builder.Services.AddSingleton<CorpusDatabase>(sp =>
{
    var corpus = sp.GetRequiredService<IOptions<CorpusOptions>>().Value;
    var factory = sp.GetRequiredService<IDbContextFactory<CorpusDbContext>>();
    return new CorpusDatabase(factory, corpus.ConnectionString);
});

builder.Services.AddSingleton<IAnalyzerDispatcher>(sp =>
{
    var ollama = ApplyCliOverrides(sp.GetRequiredService<IOptions<OllamaOptions>>().Value, filter);
    return new HardcodedAnalyzerDispatcher(ollama);
});

builder.Services.AddSingleton<IAnalysisRunner>(sp =>
{
    var db = sp.GetRequiredService<CorpusDatabase>();
    var dispatcher = sp.GetRequiredService<IAnalyzerDispatcher>();
    var ollama = ApplyCliOverrides(sp.GetRequiredService<IOptions<OllamaOptions>>().Value, filter);
    var logger = sp.GetRequiredService<ILogger<AnalysisRunner>>();
    return new AnalysisRunner(db, dispatcher, ollama, logger);
});

using var host = builder.Build();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    return command switch
    {
        "scan" => await RunScanAsync(host, cts.Token).ConfigureAwait(false),
        "analyze" => await RunAnalyzeAsync(host, filter, cts.Token).ConfigureAwait(false),
        "triage" => await RunTriageAsync(host, filter, cts.Token).ConfigureAwait(false),
        "full-analysis" => await RunFullAnalysisAsync(host, filter, cts.Token).ConfigureAwait(false),
        _ => PrintUsage(),
    };
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Cancelled.");
    return 130;
}

static int PrintUsage()
{
    Console.WriteLine("Usage: corpus-cli <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  scan            Scan source trees and load files into the database (default)");
    Console.WriteLine("  analyze         Run both triage and full analysis phases");
    Console.WriteLine("  triage          Run triage phase only (classify interest level)");
    Console.WriteLine("  full-analysis   Run full analysis on medium/high interest files only");
    Console.WriteLine();
    Console.WriteLine("Options (analyze, triage, full-analysis):");
    Console.WriteLine("  --repo <name>      Filter to exact repo name");
    Console.WriteLine("  --branch <prefix>  Filter to branches equal to or starting with prefix");
    Console.WriteLine("  --language <lang>  Filter to exact language (e.g. csharp, typescript)");
    Console.WriteLine("  --limit <n>        Cap the number of files processed");
    Console.WriteLine("  --ollama-url <url> Override Ollama base URL from config");
    Console.WriteLine("  --concurrency <n>  Override max concurrent Ollama requests");
    Console.WriteLine("  --model <name>     Override Ollama model from config");
    return 1;
}

static async Task<int> RunScanAsync(IHost host, CancellationToken ct)
{
    var options = host.Services.GetRequiredService<IOptions<CorpusOptions>>().Value;
    var db = host.Services.GetRequiredService<CorpusDatabase>();
    var walker = host.Services.GetRequiredService<SourceTreeWalker>();
    var logger = host.Services.GetRequiredService<ILogger<SourceTreeWalker>>();

    await db.EnsureSchemaAsync(ct).ConfigureAwait(false);

    var totalScanned = 0;
    var totalInserted = 0;
    var totalUpdated = 0;
    var totalUnchanged = 0;
    long totalBytes = 0;

    foreach (var source in options.Sources)
    {
        logger.LogInformation("Scanning {Repo} ({Path})...", source.Repo, source.Path);

        var buffer = new List<SourceFile>(100);
        var sourceScanned = 0;

        foreach (var file in walker.Walk(source))
        {
            ct.ThrowIfCancellationRequested();

            buffer.Add(file);
            sourceScanned++;
            totalBytes += file.SizeBytes;

            if (buffer.Count >= 100)
            {
                var stats = await db.UpsertBatchAsync(buffer, ct).ConfigureAwait(false);
                totalInserted += stats.Inserted;
                totalUpdated += stats.Updated;
                totalUnchanged += stats.Unchanged;
                buffer.Clear();
                logger.LogInformation("  {SourceScanned} files scanned...", sourceScanned);
            }
        }

        if (buffer.Count > 0)
        {
            var stats = await db.UpsertBatchAsync(buffer, ct).ConfigureAwait(false);
            totalInserted += stats.Inserted;
            totalUpdated += stats.Updated;
            totalUnchanged += stats.Unchanged;
            buffer.Clear();
        }

        totalScanned += sourceScanned;
        logger.LogInformation("  {SourceScanned} files scanned (done)", sourceScanned);
    }

    logger.LogInformation("Scan summary — scanned: {Scanned}  inserted: {Inserted}  updated: {Updated}  unchanged: {Unchanged}  size: {Size}",
        totalScanned, totalInserted, totalUpdated, totalUnchanged, FormatBytes(totalBytes));

    return 0;
}

static async Task<int> RunAnalyzeAsync(IHost host, AnalysisFilter filter, CancellationToken ct)
{
    var db = host.Services.GetRequiredService<CorpusDatabase>();
    await db.EnsureSchemaAsync(ct).ConfigureAwait(false);

    var logger = host.Services.GetRequiredService<ILogger<AnalysisRunner>>();
    LogActiveFilters(logger, filter);

    var runner = host.Services.GetRequiredService<IAnalysisRunner>();

    logger.LogInformation("=== Phase 1: Triage ===");
    var triageStats = await runner.RunTriageAsync(filter, ct).ConfigureAwait(false);
    LogAnalysisStats(logger, "Triage", triageStats);

    logger.LogInformation("=== Phase 2: Full Analysis ===");
    var fullStats = await runner.RunFullAnalysisAsync(filter, ct).ConfigureAwait(false);
    LogAnalysisStats(logger, "Full analysis", fullStats);

    return 0;
}

static async Task<int> RunTriageAsync(IHost host, AnalysisFilter filter, CancellationToken ct)
{
    var db = host.Services.GetRequiredService<CorpusDatabase>();
    await db.EnsureSchemaAsync(ct).ConfigureAwait(false);

    var logger = host.Services.GetRequiredService<ILogger<AnalysisRunner>>();
    LogActiveFilters(logger, filter);

    var runner = host.Services.GetRequiredService<IAnalysisRunner>();
    var stats = await runner.RunTriageAsync(filter, ct).ConfigureAwait(false);
    LogAnalysisStats(logger, "Triage", stats);

    return 0;
}

static async Task<int> RunFullAnalysisAsync(IHost host, AnalysisFilter filter, CancellationToken ct)
{
    var db = host.Services.GetRequiredService<CorpusDatabase>();
    await db.EnsureSchemaAsync(ct).ConfigureAwait(false);

    var logger = host.Services.GetRequiredService<ILogger<AnalysisRunner>>();
    LogActiveFilters(logger, filter);

    var runner = host.Services.GetRequiredService<IAnalysisRunner>();
    var stats = await runner.RunFullAnalysisAsync(filter, ct).ConfigureAwait(false);
    LogAnalysisStats(logger, "Full analysis", stats);

    return 0;
}

static void LogActiveFilters(ILogger logger, AnalysisFilter filter)
{
    if (filter.IsEmpty) return;

    if (filter.Repo is not null)         logger.LogInformation("Filter --repo          {Value}", filter.Repo);
    if (filter.BranchPrefix is not null) logger.LogInformation("Filter --branch        {Value}* (prefix match)", filter.BranchPrefix);
    if (filter.Language is not null)     logger.LogInformation("Filter --language      {Value}", filter.Language);
    if (filter.Limit is not null)        logger.LogInformation("Filter --limit         {Value}", filter.Limit);
    if (filter.OllamaUrl is not null)    logger.LogInformation("Filter --ollama-url    {Value}", filter.OllamaUrl);
    if (filter.Concurrency is not null)  logger.LogInformation("Filter --concurrency   {Value}", filter.Concurrency);
    if (filter.Model is not null)        logger.LogInformation("Filter --model         {Value}", filter.Model);
}

static void LogAnalysisStats(ILogger logger, string phase, AnalysisStats stats)
{
    logger.LogInformation("{Phase} summary — processed: {Processed}  failed: {Failed}  high: {High}  medium: {Medium}  low: {Low}",
        phase, stats.Processed, stats.Failed, stats.High, stats.Medium, stats.Low);
}

static string FormatBytes(long bytes) => bytes switch
{
    >= 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
    >= 1024 * 1024         => $"{bytes / (1024.0 * 1024):F1} MB",
    >= 1024                => $"{bytes / 1024.0:F1} KB",
    _                      => $"{bytes} B",
};

// CLI flags take precedence over config-file values; applied at construction time
// because OllamaOptions uses init-only properties.
static OllamaOptions ApplyCliOverrides(OllamaOptions options, AnalysisFilter filter)
{
    var baseUrl = filter.OllamaUrl ?? options.BaseUrl;
    var model = filter.Model ?? options.Model;
    var concurrency = filter.Concurrency ?? options.MaxConcurrency;

    if (baseUrl == options.BaseUrl && model == options.Model && concurrency == options.MaxConcurrency)
        return options;

    return new OllamaOptions
    {
        BaseUrl = baseUrl,
        Model = model,
        MaxConcurrency = concurrency,
    };
}
