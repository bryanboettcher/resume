---
title: Entity Framework Core Patterns — Schema Management and Typed Queries (resume-chat corpus CLI)
tags: [entity-framework-core, ef-core, csharp, postgresql, npgsql, schema-management, fluent-api, on-conflict, orm-selection]
related:
  - evidence/entity-framework-core-patterns.md
  - evidence/entity-framework-core-patterns-saga-persistence.md
  - evidence/entity-framework-core-patterns-reporting.md
  - evidence/dapper-async-data-access.md
  - projects/resume-chatbot.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/entity-framework-core-patterns.md
---

# Entity Framework Core Patterns — Schema Management and Typed Queries (resume-chat)

The resume chatbot's corpus analysis CLI uses EF Core for schema definition and typed queries against PostgreSQL. It also demonstrates the same dual-ORM pragmatism seen in madera-apps: EF Core for schema creation and typed queries, raw Npgsql for upsert semantics that EF Core does not support natively.

---

## Evidence: Schema Management with Fluent API

The `CorpusDbContext` maps a four-table schema with explicit column naming, index definitions, and foreign key cascades using the fluent API:

```csharp
// resume-chat:src/ResumeChat.Corpus.Cli/CorpusDbContext.cs
modelBuilder.Entity<SourceFileEntity>(e =>
{
    e.ToTable("source_files");
    e.Property(x => x.ContentHash).HasColumnName("content_hash").IsRequired();
    e.HasIndex(x => new { x.Repo, x.Branch }).HasDatabaseName("idx_source_files_repo_branch");
    e.HasAlternateKey(x => new { x.Repo, x.Branch, x.FilePath });
});
```

---

## Evidence: EF Core for Queries, Raw Npgsql for Upserts

The `CorpusDatabase` class shows the dual-ORM approach. EF Core handles schema creation and typed query composition:

```csharp
// resume-chat:src/ResumeChat.Corpus.Cli/CorpusDatabase.cs
var query = ctx.SourceFiles
    .Where(sf => !ctx.FileAnalyses.Any(fa => fa.SourceFileId == sf.Id && fa.AnalysisType == analysisType));

if (filter.Repo is not null)
    query = query.Where(sf => sf.Repo == filter.Repo);
```

But for bulk upserts, it drops to raw Npgsql with `ON CONFLICT` semantics that EF Core does not support natively:

```csharp
// resume-chat:src/ResumeChat.Corpus.Cli/CorpusDatabase.cs
// Raw Npgsql for ON CONFLICT upsert — EF Core has no first-class support for this.
await using var conn = new NpgsqlConnection(connectionString);
```

The upsert uses PostgreSQL's `xmax` system column to distinguish inserts from updates in the `RETURNING` clause — a database-specific technique that would be lost behind EF Core's abstraction.

EF Core also handles the schema bootstrap via `EnsureCreatedAsync`, followed by raw SQL to add a named constraint that EF Core's `HasAlternateKey` cannot target for `ON CONFLICT`:

```csharp
await ctx.Database.EnsureCreatedAsync(ct);
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
    """, ct);
```

---

## The Decision Framework: When EF Core, When Dapper

Across all three codebases, the ORM selection follows a consistent pattern:

**Use EF Core when:**
- MassTransit saga persistence needs change tracking and optimistic concurrency (kb-platform `SagaDbContext`)
- Reporting queries require composable LINQ with dynamic filters and GROUP BY aggregations (madera-apps `EfCorePaginatedServiceBase`)
- Schema definition and typed query composition against normalized tables (resume-chat `CorpusDbContext`)
- The query shape is determined at runtime by optional filters

**Use Dapper (or raw ADO.NET) when:**
- Stored procedures are the primary access pattern (madera-apps pipeline)
- Table-valued parameters or bulk operations require ADO.NET primitives
- Streaming via `IAsyncEnumerable` needs unbuffered query execution
- Saga persistence requires explicit transaction isolation control (madera-apps SQL Server sagas)
- Upsert semantics (`ON CONFLICT`, `MERGE`) have no EF Core equivalent
- Performance-critical paths where change tracking overhead is wasteful

---

## Key Files

- `resume-chat:src/ResumeChat.Corpus.Cli/CorpusDbContext.cs` — Fluent API schema definition with explicit column/index naming
- `resume-chat:src/ResumeChat.Corpus.Cli/CorpusDatabase.cs` — EF Core for queries, raw Npgsql for ON CONFLICT upserts
