using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ResumeChat.Corpus.Cli;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Services.AddOptions<CorpusOptions>()
    .BindConfiguration(CorpusOptions.SectionName)
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

using var host = builder.Build();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var options = host.Services.GetRequiredService<IOptions<CorpusOptions>>().Value;
var db = host.Services.GetRequiredService<CorpusDatabase>();
var walker = host.Services.GetRequiredService<SourceTreeWalker>();

try
{
    await db.EnsureSchemaAsync(cts.Token).ConfigureAwait(false);

    var totalScanned = 0;
    var totalInserted = 0;
    var totalUpdated = 0;
    var totalUnchanged = 0;
    long totalBytes = 0;

    foreach (var source in options.Sources)
    {
        Console.WriteLine($"Scanning {source.Repo} ({source.Path})...");

        var buffer = new List<SourceFile>(100);
        var sourceScanned = 0;

        void FlushProgress()
        {
            Console.Write($"\r  {sourceScanned} files scanned...");
        }

        foreach (var file in walker.Walk(source))
        {
            cts.Token.ThrowIfCancellationRequested();

            buffer.Add(file);
            sourceScanned++;
            totalBytes += file.SizeBytes;

            if (buffer.Count >= 100)
            {
                var stats = await db.UpsertBatchAsync(buffer, cts.Token).ConfigureAwait(false);
                totalInserted += stats.Inserted;
                totalUpdated += stats.Updated;
                totalUnchanged += stats.Unchanged;
                buffer.Clear();
                FlushProgress();
            }
        }

        if (buffer.Count > 0)
        {
            var stats = await db.UpsertBatchAsync(buffer, cts.Token).ConfigureAwait(false);
            totalInserted += stats.Inserted;
            totalUpdated += stats.Updated;
            totalUnchanged += stats.Unchanged;
            buffer.Clear();
        }

        totalScanned += sourceScanned;
        Console.WriteLine($"\r  {sourceScanned} files scanned.    ");
    }

    Console.WriteLine();
    Console.WriteLine("Summary");
    Console.WriteLine($"  Files scanned:  {totalScanned,8:N0}");
    Console.WriteLine($"  Inserted:       {totalInserted,8:N0}");
    Console.WriteLine($"  Updated:        {totalUpdated,8:N0}");
    Console.WriteLine($"  Unchanged:      {totalUnchanged,8:N0}");
    Console.WriteLine($"  Total size:     {FormatBytes(totalBytes),8}");

    return 0;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Cancelled.");
    return 130;
}

static string FormatBytes(long bytes) => bytes switch
{
    >= 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
    >= 1024 * 1024         => $"{bytes / (1024.0 * 1024):F1} MB",
    >= 1024                => $"{bytes / 1024.0:F1} KB",
    _                      => $"{bytes} B",
};
