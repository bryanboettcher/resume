---
title: Custom Database Migration Strategy with Embedded SQL Resources
tags: [sql-server, schema-migrations, database-design, devops, deployment, docker, csharp]
related:
  - evidence/sql-server-database-engineering.md
  - evidence/sql-server-database-engineering-schema-migrations.md
  - evidence/dependency-injection-composition.md
  - projects/call-trader-madera.md
children:
  - evidence/database-migration-strategy-runner-design.md
  - evidence/database-migration-strategy-deployment.md
category: evidence
contact: resume@bryanboettcher.com
---

# Custom Database Migration Strategy with Embedded SQL Resources — Index

The Madera/Call-Trader platform uses a custom-built database migration system rather than EF Core migrations or third-party tools like DbUp or FluentMigrator. The system manages schema evolution across a SQL Server 2022 database powering a direct mail lead generation pipeline. Over seven months (December 2024 through August 2025), 26 distinct migration batches containing 200+ individual SQL scripts were applied through this framework.

Key design decisions:

- **Embedded resources over file-based**: SQL scripts are compiled into the assembly via MSBuild, eliminating deployment-time file management. The runner works identically locally, in Docker, or in Kubernetes.
- **Date-based versioning**: `YYYYMMDD` directory names sort lexicographically, avoid merge conflicts, and are human-readable.
- **Dual implementation**: A standalone Docker container runs as a Kubernetes Job before pods start; the same runner is also embedded in the application for startup-time self-healing.
- **Not EF Core**: The database layer is heavily procedural — stored procedures, table-valued parameters, partitioned tables, UDFs, views. EF Core migrations would fight those patterns.

The full evidence is split into focused documents:

## Child Documents

- **[Runner Design](database-migration-strategy-runner-design.md)** — MSBuild embedded resource compilation, `Migrations/{Database}/{YYYYMMDD}/` directory structure as version system, numeric-prefix execution ordering, `DbMigrator` lifecycle (schema table bootstrap, version discovery, script execution, result tracking, SQL InfoMessage logging).

- **[Dual Deployment, Historical Seeding, and Scale](database-migration-strategy-deployment.md)** — The standalone `Madera.DbMigrations` Kubernetes Job container vs. the `Madera.Workflows` application-embedded runner with DI-based `MigrationRegistry`. Historical version seeding for databases that predate the framework. The 26-batch migration history and MongoDB companion migrator.
