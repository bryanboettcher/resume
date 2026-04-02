using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ResumeChat.Corpus.Cli;

sealed class CorpusDatabase(IDbContextFactory<CorpusDbContext> contextFactory, string connectionString)
{
    private const int BatchSize = 100;

    // EF Core creates tables and indexes; raw DDL handles the unique constraint
    // EnsureCreated doesn't run migrations, but for a greenfield schema it's the right fit.
    public async Task EnsureSchemaAsync(CancellationToken ct)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await ctx.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);

        // EF Core HasAlternateKey creates a unique index named after the columns, but we
        // need the named constraint for ON CONFLICT targeting. Patch it to be idempotent.
        await ctx.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'uq_source_files_repo_branch_path'
                ) THEN
                    ALTER TABLE source_files
                        ADD CONSTRAINT uq_source_files_repo_branch_path
                        UNIQUE (repo, branch, file_path);
                END IF;
            END$$;
            """, ct).ConfigureAwait(false);
    }

    public async Task<UpsertStats> UpsertBatchAsync(IReadOnlyList<SourceFile> files, CancellationToken ct)
    {
        var inserted = 0;
        var updated = 0;
        var unchanged = 0;

        // Raw Npgsql for ON CONFLICT upsert — EF Core has no first-class support for this.
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);

        for (var i = 0; i < files.Count; i += BatchSize)
        {
            var batch = files.Skip(i).Take(BatchSize).ToList();
            var (batchInserted, batchUpdated, batchUnchanged) =
                await UpsertChunkAsync(conn, batch, ct).ConfigureAwait(false);

            inserted += batchInserted;
            updated += batchUpdated;
            unchanged += batchUnchanged;
        }

        return new UpsertStats(inserted, updated, unchanged);
    }

    private static async Task<(int Inserted, int Updated, int Unchanged)> UpsertChunkAsync(
        NpgsqlConnection conn,
        IReadOnlyList<SourceFile> chunk,
        CancellationToken ct)
    {
        // Build a single multi-row INSERT with ON CONFLICT DO UPDATE.
        // xmax = 0 means the row was inserted; xmax != 0 means it was updated.
        // We only update when content_hash differs to avoid touching unchanged rows.
        var sb = new System.Text.StringBuilder();
        sb.Append("""
            INSERT INTO source_files (repo, branch, file_path, language, content_text, content_hash, line_count, size_bytes, scanned_at)
            VALUES
            """);

        var parameters = new List<NpgsqlParameter>(chunk.Count * 8);

        for (var j = 0; j < chunk.Count; j++)
        {
            var f = chunk[j];
            var p = j * 8;
            if (j > 0) sb.Append(',');
            sb.Append($"""
            (${p + 1},${p + 2},${p + 3},${p + 4},${p + 5},${p + 6},${p + 7},${p + 8},NOW())
            """);

            parameters.Add(new NpgsqlParameter { Value = f.Repo });
            parameters.Add(new NpgsqlParameter { Value = f.Branch });
            parameters.Add(new NpgsqlParameter { Value = f.FilePath });
            parameters.Add(new NpgsqlParameter { Value = (object?)f.Language ?? DBNull.Value });
            parameters.Add(new NpgsqlParameter { Value = f.ContentText });
            parameters.Add(new NpgsqlParameter { Value = f.ContentHash });
            parameters.Add(new NpgsqlParameter { Value = f.LineCount });
            parameters.Add(new NpgsqlParameter { Value = f.SizeBytes });
        }

        sb.Append("""

            ON CONFLICT ON CONSTRAINT uq_source_files_repo_branch_path DO UPDATE
            SET content_text  = EXCLUDED.content_text,
                content_hash  = EXCLUDED.content_hash,
                language      = EXCLUDED.language,
                line_count    = EXCLUDED.line_count,
                size_bytes    = EXCLUDED.size_bytes,
                scanned_at    = NOW()
            WHERE source_files.content_hash <> EXCLUDED.content_hash
            RETURNING (xmax = 0)::int AS was_inserted, (xmax <> 0)::int AS was_updated
            """);

        await using var cmd = new NpgsqlCommand(sb.ToString(), conn);
        foreach (var p in parameters)
            cmd.Parameters.Add(p);

        var inserted = 0;
        var updated = 0;

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            inserted += reader.GetInt32(0);
            updated += reader.GetInt32(1);
        }

        // Rows not returned by RETURNING were skipped (unchanged)
        var unchanged = chunk.Count - inserted - updated;
        return (inserted, updated, unchanged);
    }

    public async Task<List<UnanalyzedFile>> GetFilesWithoutAnalysisAsync(string analysisType, AnalysisFilter filter, CancellationToken ct)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var query = ctx.SourceFiles
            .Where(sf => !ctx.FileAnalyses.Any(fa => fa.SourceFileId == sf.Id && fa.AnalysisType == analysisType));

        if (filter.Repo is not null)
            query = query.Where(sf => sf.Repo == filter.Repo);

        if (filter.BranchPrefix is not null)
            query = query.Where(sf => sf.Branch == filter.BranchPrefix || sf.Branch.StartsWith(filter.BranchPrefix));

        if (filter.Language is not null)
            query = query.Where(sf => sf.Language == filter.Language);

        var ordered = query
            .OrderBy(sf => sf.Repo).ThenBy(sf => sf.Branch).ThenBy(sf => sf.FilePath)
            .Select(sf => new UnanalyzedFile(sf.Id, sf.Repo, sf.Branch, sf.FilePath, sf.Language, sf.ContentText, sf.LineCount));

        if (filter.Limit is not null)
            ordered = ordered.Take(filter.Limit.Value);

        return await ordered.ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<List<TriagedFile>> GetTriagedFilesNeedingFullAnalysisAsync(AnalysisFilter filter, CancellationToken ct)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Keep all filtering, ordering, and limiting on entity types before projecting into
        // TriagedFile — EF Core cannot translate predicates or ordering on projected record types.
        var sourceFiles = ctx.SourceFiles.AsQueryable();

        if (filter.Repo is not null)
            sourceFiles = sourceFiles.Where(sf => sf.Repo == filter.Repo);

        if (filter.BranchPrefix is not null)
            sourceFiles = sourceFiles.Where(sf => sf.Branch == filter.BranchPrefix || sf.Branch.StartsWith(filter.BranchPrefix));

        if (filter.Language is not null)
            sourceFiles = sourceFiles.Where(sf => sf.Language == filter.Language);

        // Get all triage records for files that don't yet have full_analysis
        var pairs = ctx.FileAnalyses
            .Where(fa => fa.AnalysisType == "triage")
            .Where(fa => !ctx.FileAnalyses.Any(fa2 => fa2.SourceFileId == fa.SourceFileId && fa2.AnalysisType == "full_analysis"))
            .Join(sourceFiles, fa => fa.SourceFileId, sf => sf.Id, (fa, sf) => new { fa, sf })
            .OrderBy(x => x.sf.Repo).ThenBy(x => x.sf.Branch).ThenBy(x => x.sf.FilePath);

        var limited = filter.Limit is not null ? pairs.Take(filter.Limit.Value) : pairs;

        return await limited
            .Select(x => new TriagedFile(x.sf.Id, x.sf.Repo, x.sf.Branch, x.sf.FilePath, x.sf.Language, x.sf.ContentText, x.sf.LineCount, x.fa.ContentText))
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task InsertAnalysisAsync(long sourceFileId, string analyzer, string analysisType, string contentJson, CancellationToken ct)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        ctx.FileAnalyses.Add(new FileAnalysisEntity
        {
            SourceFileId = sourceFileId,
            Analyzer = analyzer,
            AnalysisType = analysisType,
            ContentText = contentJson,
        });

        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task InsertTagsAsync(long sourceFileId, string analyzer, IReadOnlyList<string> tags, CancellationToken ct)
    {
        if (tags.Count == 0) return;

        await using var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        foreach (var tag in tags)
        {
            ctx.FileTags.Add(new FileTagEntity
            {
                SourceFileId = sourceFileId,
                Tag = tag,
                Analyzer = analyzer,
            });
        }

        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

readonly record struct UpsertStats(int Inserted, int Updated, int Unchanged);

sealed record UnanalyzedFile(
    long Id, string Repo, string Branch, string FilePath,
    string? Language, string ContentText, int LineCount);

sealed record TriagedFile(
    long Id, string Repo, string Branch, string FilePath,
    string? Language, string ContentText, int LineCount,
    string TriageJson);
