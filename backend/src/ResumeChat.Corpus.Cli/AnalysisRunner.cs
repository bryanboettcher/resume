using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ResumeChat.Corpus.Cli;

sealed class AnalysisRunner(CorpusDatabase db, IAnalyzerDispatcher dispatcher, OllamaOptions options, ILogger<AnalysisRunner> logger) : IAnalysisRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
    };

    public async Task<AnalysisStats> RunTriageAsync(AnalysisFilter filter, CancellationToken ct)
    {
        var files = await db.GetFilesWithoutAnalysisAsync("triage", filter, ct).ConfigureAwait(false);
        logger.LogInformation("Triage: {Count} files with no triage result", files.Count);
        if (files.Count == 0) return new AnalysisStats();

        var stats = new AnalysisStats();

        // Dimension hit counters — incremented under lock(stats) alongside other stats.
        var hasLogicCount = 0;
        var hasDomainRulesCount = 0;
        var hasCompositionCount = 0;
        var hasDataModelingCount = 0;

        var concurrency = Math.Max(1, options.MaxConcurrency);
        var semaphore = new SemaphoreSlim(concurrency, concurrency);
        var processed = 0;

        var tasks = files.Select(async file =>
        {
            await semaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                ct.ThrowIfCancellationRequested();

                var current = Interlocked.Increment(ref processed);

                var analyzer = dispatcher.GetAnalyzer(file.Language);
                var observation = await analyzer.TriageDetailAsync(file.FilePath, file.Language ?? "unknown", file.ContentText, ct).ConfigureAwait(false);
                if (observation is null)
                {
                    lock (stats)
                        stats.Failed++;

                    logger.LogWarning("[{Current}/{Total}] {Repo}/{FilePath} — FAILED: no valid response",
                        current, files.Count, file.Repo, file.FilePath);
                    return;
                }

                var json = JsonSerializer.Serialize(observation, JsonOptions);
                await db.InsertAnalysisAsync(file.Id, options.Model, "triage", json, ct).ConfigureAwait(false);

                var anyTrue = observation.HasLogic || observation.HasDomainRules || observation.HasComposition || observation.HasDataModeling;

                // Truncate reasoning so the operator can see what the model is thinking at a glance.
                var snippet = observation.Reasoning is { Length: > 0 } r
                    ? (r.Length > 80 ? r[..80] + "..." : r)
                    : "(no reasoning)";

                var dimensions = string.Concat(
                    observation.HasLogic         ? "L" : "-",
                    observation.HasDomainRules    ? "D" : "-",
                    observation.HasComposition    ? "C" : "-",
                    observation.HasDataModeling   ? "M" : "-");

                logger.LogInformation("[{Current}/{Total}] {Repo}/{FilePath} → {Dimensions} \"{Snippet}\"",
                    current, files.Count, file.Repo, file.FilePath, dimensions, snippet);

                lock (stats)
                {
                    stats.Processed++;
                    if (anyTrue) stats.Medium++; else stats.Low++;

                    if (observation.HasLogic) hasLogicCount++;
                    if (observation.HasDomainRules) hasDomainRulesCount++;
                    if (observation.HasComposition) hasCompositionCount++;
                    if (observation.HasDataModeling) hasDataModelingCount++;
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        logger.LogInformation("Dimension hits: logic={Logic}  domain_rules={DomainRules}  composition={Composition}  data_modeling={DataModeling}",
            hasLogicCount, hasDomainRulesCount, hasCompositionCount, hasDataModelingCount);

        return stats;
    }

    public async Task<AnalysisStats> RunFullAnalysisAsync(AnalysisFilter filter, CancellationToken ct)
    {
        var triaged = await db.GetTriagedFilesNeedingFullAnalysisAsync(filter, ct).ConfigureAwait(false);

        // Filter to medium/high interest by checking whether any observation dimension was true.
        // The triage JSON is now a TriageObservation; any dimension true means worth analyzing.
        var files = triaged.Where(f =>
        {
            try
            {
                var observation = JsonSerializer.Deserialize<TriageObservation>(f.TriageJson, JsonOptions);
                return observation is { } o && (o.HasLogic || o.HasDomainRules || o.HasComposition || o.HasDataModeling);
            }
            catch { return false; }
        }).ToList();

        logger.LogInformation("Full analysis: {Count} medium/high interest files (of {TriagedCount} triaged without full analysis)",
            files.Count, triaged.Count);
        if (files.Count == 0) return new AnalysisStats();

        var stats = new AnalysisStats();
        var concurrency = Math.Max(1, options.MaxConcurrency);
        var semaphore = new SemaphoreSlim(concurrency, concurrency);
        var processed = 0;

        var tasks = files.Select(async file =>
        {
            await semaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                ct.ThrowIfCancellationRequested();

                var current = Interlocked.Increment(ref processed);

                var analyzer = dispatcher.GetAnalyzer(file.Language);
                var result = await analyzer.AnalyzeAsync(file.FilePath, file.Language ?? "unknown", file.ContentText, ct).ConfigureAwait(false);
                if (result is null)
                {
                    lock (stats)
                        stats.Failed++;

                    logger.LogWarning("[{Current}/{Total}] {Repo}/{FilePath} — FAILED: no valid response",
                        current, files.Count, file.Repo, file.FilePath);
                    return;
                }

                logger.LogInformation("[{Current}/{Total}] {Repo}/{FilePath} → {Complexity}",
                    current, files.Count, file.Repo, file.FilePath, result.Complexity);

                var json = JsonSerializer.Serialize(result, JsonOptions);
                await db.InsertAnalysisAsync(file.Id, options.Model, "full_analysis", json, ct).ConfigureAwait(false);
                await db.InsertTagsAsync(file.Id, options.Model, result.ResumeKeywords, ct).ConfigureAwait(false);

                lock (stats)
                {
                    stats.Processed++;
                    switch (result.Complexity)
                    {
                        case "high": stats.High++; break;
                        case "medium": stats.Medium++; break;
                        default: stats.Low++; break;
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return stats;
    }
}

sealed class AnalysisStats
{
    public int Processed { get; set; }
    public int Failed { get; set; }
    public int High { get; set; }
    public int Medium { get; set; }
    public int Low { get; set; }
}
