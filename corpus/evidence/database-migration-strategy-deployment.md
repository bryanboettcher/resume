---
title: Custom Database Migration Strategy — Dual Deployment, Historical Seeding, and Scale
tags: [sql-server, schema-migrations, database-design, docker, kubernetes, deployment, devops, csharp, mongodb, di-pattern]
related:
  - evidence/database-migration-strategy.md
  - evidence/database-migration-strategy-runner-design.md
  - evidence/sql-server-database-engineering.md
  - evidence/dependency-injection-composition.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/database-migration-strategy.md
---

# Custom Database Migration Strategy — Dual Deployment, Historical Seeding, and Scale

This document covers how the migration runner is deployed in two parallel implementations (Kubernetes Job + application startup), how it handles databases that predate the framework, and the full migration history.

---

## Evidence: Two Parallel Implementations

The migration runner exists in two locations serving different deployment contexts:

**`Madera.DbMigrations`** — A standalone console application with its own `Dockerfile`, built as a separate container image. `Program.cs` iterates over four database names (`DirectMail`, `Convoso`, `Dispos`, `Ringba`), passing each connection string and database name to `DbMigrator.MigrateSchema(connectionString, database)`. The database name parameter filters which embedded resources apply: `GetMigrationVersions` filters manifests starting with `Madera.DbMigrations.Migrations.{database}`. This container runs as a Kubernetes Job during deployment, applying migrations before the application pods start.

**`Madera.Workflows`** — The same `DbMigrator` class embedded directly in the application, wired through a `MigrationRegistry` that uses DI to construct migrators from connection-string-bearing options types:

```csharp
private static DbMigrator BuildFrom<TOptions>(IServiceProvider sp)
    where TOptions : class, IConnectionStringProvider
{
    var options = sp.GetService(typeof(IOptions<>).MakeGenericType(typeof(TOptions)))
        as IOptions<TOptions>;
    var logger = sp.GetService(typeof(ILogger<>).MakeGenericType(typeof(TOptions)))
        as ILogger;
    return new DbMigrator(options.Value.ConnectionString, logger);
}
```

The Workflows version constructs migrators for each dataflow subsystem (`DirectMailOptions`, `RingbaOptions`, `ConvosoOptions`, `DispoOptions`), with each options class implementing `IConnectionStringProvider`. This enables application-startup migration — the schema is guaranteed current before any MassTransit consumers start processing messages.

The `[ExcludeFromScanner]` attribute on the Workflows `DbMigrator` prevents Lamar's assembly scanner from auto-registering it as a service, since it requires explicit construction with the correct connection string.

---

## Evidence: Historical Version Seeding

A notable pragmatic decision appears in `QuerySchemaVersions`. When the `SchemaVersion` table already exists (meaning the database predates the migration framework), the code hard-seeds the first 10 known versions:

```csharp
if (!created)
{
    // seed existing migrations since we don't have a historical context,
    // but we don't pre-seed if this database has *never* been migrated
    versions.Add("20241227");
    versions.Add("20241228");
    versions.Add("20250106");
    // ... through 20250207
}
```

This handles the bootstrap problem: the migration framework was introduced after the database already had schema applied through earlier versions. New databases (where the table didn't exist and was just created) don't get seeded, so they receive the full migration history from the beginning. The `20250220` migration batch itself adds `StartedOn`, `CompletedOn`, and `ExceptionMessage` columns to `SchemaVersion` — this is the point where the framework became self-tracking.

---

## Evidence: Migration Scale and Evolution

The 26 migration batches across seven months show a clear progression in the system's sophistication:

| Period | Batches | Character |
|--------|---------|-----------|
| Dec 2024 | 2 | Initial schema: 37 scripts establishing schemas, reference data, core tables, staging with partitioning, stored procedures, views |
| Jan 2025 | 4 | Scrub pipeline introduction, CRC32 hashing functions, data model restructuring |
| Feb 2025 | 6 | Index tuning, reporting procedures, schema version tracking, reference data updates |
| Mar 2025 | 4 | View refinements, publisher data |
| Apr 2025 | 3 | Major overhaul (25-script batch): table restructuring via create-migrate-drop-rename, stored procedure rewrites, reporting layer |
| May 2025 | 2 | UDF extraction for state descriptions, Ringba call log integration |
| Jun-Aug 2025 | 3 | Mail planning reports, late-stage refinements |

The `Madera.DbMigrations` project also includes a `MongoMigrator` that handles Hangfire MongoDB schema migrations using `Hangfire.Mongo`'s built-in `MongoMigrationManager` with a `CollectionMongoBackupStrategy`. This runs before the SQL migrations in `Program.cs`, ensuring the background job infrastructure is current before the application databases are migrated.

---

## Key Files

- `madera-apps:Madera/Madera.DbMigrations/Program.cs` — Console app orchestrating migrations across four databases
- `madera-apps:Madera/Madera.DbMigrations/Dockerfile` — Multi-stage Docker build for migration container
- `madera-apps:Madera/Madera.Workflows/Migrations/DbMigrator.cs` — Application-embedded runner with DI-based `MigrationRegistry`
- `madera-apps:Madera/Madera.DbMigrations/MongoMigrator.cs` — Hangfire MongoDB migration companion
- `madera-apps:Madera/Madera.DbMigrations/Migrations/DirectMail/20241227/` — Initial schema batch (37 scripts)
- `madera-apps:Madera/Madera.DbMigrations/Migrations/DirectMail/20250407/` — Largest migration batch (25 scripts, major restructure)
