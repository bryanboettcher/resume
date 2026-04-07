---
title: Custom Database Migration Strategy — Runner Design (Embedded Resources, Directory Structure, DbMigrator)
tags: [sql-server, schema-migrations, database-design, csharp, embedded-resources, msbuild, versioning, devops]
related:
  - evidence/database-migration-strategy.md
  - evidence/database-migration-strategy-deployment.md
  - evidence/sql-server-database-engineering.md
  - evidence/sql-server-database-engineering-schema-migrations.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/database-migration-strategy.md
---

# Custom Database Migration Strategy — Runner Design

The Madera/Call-Trader platform uses a custom-built database migration system rather than EF Core migrations or third-party tools like DbUp or FluentMigrator. This document covers how migration scripts are compiled into the assembly, how they are versioned by directory structure, and how the `DbMigrator` class executes them.

---

## Evidence: Embedded Resource Compilation via MSBuild

The `.csproj` for `Madera.DbMigrations` uses a custom MSBuild target to convert all `.sql` files under `Migrations/` into embedded assembly resources at build time:

```xml
<Target Name="SqlToResources" BeforeTargets="BeforeBuild">
  <CreateItem Include="Migrations\**\**\*.sql">
    <Output ItemName="EmbeddedResource" TaskParameter="Include" />
  </CreateItem>
</Target>
```

This means the compiled assembly is entirely self-contained — no external SQL files to deploy or lose. The SQL scripts are baked into the DLL, and the migration runner discovers them via `Assembly.GetExecutingAssembly().GetManifestResourceNames()`.

---

## Evidence: Directory Structure as Version System

Migration scripts are organized in a `Migrations/{Database}/{YYYYMMDD}/` hierarchy. The date-stamped directories serve as version identifiers. Within each directory, scripts are numbered with a prefix that controls execution order:

- `01_` — Schema creation (e.g., `01_schema_data.sql`, `01_schema_ref.sql`)
- `02_` — Reference data tables (e.g., `02_ref_Brokers.sql`, `02_ref_Publishers.sql`)
- `03_` — Data tables (e.g., `03_data_Leads.sql`, `03_data_Recipients.sql`)
- `04_` — Staging tables (e.g., `04_stage_Recipients.sql`)
- `05_` — Table-valued parameters (e.g., `05_tvp_NormalizedAddressType.sql`)
- `06_` — Stored procedures (e.g., `06_usp_MigrateRecipients.sql`)
- `07_` — Views (e.g., `07_vw_importlog_report.sql`)

Later migration batches evolved to an alphanumeric suffix convention (`01A_`, `01B_`, `02A_`, etc.) allowing finer interleaving within a single batch. The `20250407` batch — one of the largest at 25 scripts — uses `01A_` through `17_` to orchestrate a major overhaul that drops views, drops stored procedures, restructures tables, and recreates everything in the correct dependency order.

---

## Evidence: The DbMigrator Class

`DbMigrator.cs` implements the full migration lifecycle:

1. **Schema table bootstrap**: `EnsureSchemaTableExists` checks `information_schema.tables` for `dbo.SchemaVersion`. If absent, it creates the tracking table with `CREATE TABLE dbo.SchemaVersion (Version CHAR(8) NOT NULL)`.

2. **Version discovery**: `GetMigrationVersions` extracts the 8-character date from embedded resource names using `name.Split('.', 6)[4]`, filters against already-applied versions, and sorts them chronologically.

3. **Script execution**: For each outstanding version, `RunMigrationsForVersion` iterates over embedded resources matching that version (via `manifest.Contains($"._{version}.")`) in sorted order, reads each SQL script via `StreamReader`, and executes it with a 4-hour timeout (`TimeSpan.FromHours(4).TotalSeconds`).

4. **Result tracking**: Each version batch records `StartedOn`, `CompletedOn`, and `ExceptionMessage` in `dbo.SchemaVersion`. Failures halt further migration batches but still record the failed version's results.

5. **Error isolation**: If any individual script within a version batch throws a `SqlException`, execution stops for that batch and subsequent batches. The exception message is captured and persisted, enabling diagnosis without losing the audit trail.

SQL InfoMessage logging bridges T-SQL `PRINT` statements into the .NET logging pipeline:

```csharp
connection.InfoMessage += (s, e) =>
{
    _logger.LogInformation(
        "  Migration {migrationVersion}/{scriptName}: {message}",
        _migrationVersion, _scriptName, e.Message
    );
};
```

This means migration progress appears in the same structured log stream as the rest of the application.

---

## Key Files

- `madera-apps:Madera/Madera.DbMigrations/DbMigrator.cs` — Standalone migration runner (277 lines)
- `madera-apps:Madera/Madera.Workflows/Migrations/DbMigrator.cs` — Application-embedded migration runner (309 lines)
- `madera-apps:Madera/Madera.DbMigrations/Madera.DbMigrations.csproj` — MSBuild target converting SQL files to embedded resources
