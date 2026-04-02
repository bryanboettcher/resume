---
title: SQL Server Database Engineering
tags: [sql-server, stored-procedures, columnstore-indexes, schema-migrations, bitflags, partitioning, snapshot-isolation, indexing-strategy, database-design, reporting-pipelines]
related:
  - evidence/data-engineering-etl.md
  - evidence/etl-pipeline-framework.md
  - projects/call-trader-madera.md
children:
  - evidence/sql-server-database-engineering-bitflag-architecture.md
  - evidence/sql-server-database-engineering-columnstore-indexes.md
  - evidence/sql-server-database-engineering-schema-migrations.md
  - evidence/sql-server-database-engineering-fillfactor-tuning.md
  - evidence/sql-server-database-engineering-partitioned-staging.md
  - evidence/sql-server-database-engineering-snapshot-isolation.md
  - evidence/sql-server-database-engineering-staging-pipeline.md
  - evidence/sql-server-database-engineering-analytical-reporting.md
category: evidence
contact: resume@bryanboettcher.com
---

# SQL Server Database Engineering — Index

The Madera/Call-Trader platform (madera-apps repository) manages a direct mail lead generation pipeline processing 30M+ recipient records. The SQL Server 2022 database layer is a substantial engineering artifact: 150+ SQL migration files spanning 26 migration batches from December 2024 through August 2025. Features include `GET_BIT`/`SET_BIT` bitflag architecture, nonclustered columnstore indexes on OLTP tables, partitioned staging tables, FILLFACTOR-tuned indexes, SNAPSHOT isolation throughout the reporting layer, and zero-downtime rename migrations.

The C# pipeline code that drives these stored procedures is covered in `evidence/data-engineering-etl.md`.

## Child Documents

- **[Bitflag Architecture for Address and Scrub Metadata](sql-server-database-engineering-bitflag-architecture.md)** — 17 computed columns via `GET_BIT` decomposing USPS address verification flags from a single `INT`; parallel `SET_BIT` pattern for multi-reason scrub tracking with a `ref.ScrubbedReasons` bit layout table. Uses SQL Server 2022 `GET_BIT`/`SET_BIT` instead of manual bitmask arithmetic for auditability on TCPA compliance decisions.

- **[Columnstore Indexes on OLTP Data](sql-server-database-engineering-columnstore-indexes.md)** — Hybrid OLTP/OLAP pattern on `data.Leads`: row-store indexes for the write path, columnstore on `ImportId` for batch-mode analytical reporting. The same pattern applied to `#import_recipients` temp tables shared across scrub sub-procedures.

- **[Zero-Downtime Schema Migration via Rename Pattern](sql-server-database-engineering-schema-migrations.md)** — Create-migrate-drop-rename pattern using `sp_rename` for large table restructures: avoids long schema locks from `ALTER TABLE` type changes. The April 2025 migration simultaneously changes the PK type, replaces the FK with direct GUID correlation, and renames the saga table for domain alignment.

- **[FILLFACTOR and OPTIMIZE_FOR_SEQUENTIAL_KEY Tuning](sql-server-database-engineering-fillfactor-tuning.md)** — Workload-based fill factor decisions: 60% on GUID-keyed NCIs (scattered inserts), 95% on ascending CreatedOn clustered indexes with `OPTIMIZE_FOR_SEQUENTIAL_KEY`, 50% on the write-heavy `RecipientId` lookup index.

- **[Partitioned Staging Tables](sql-server-database-engineering-partitioned-staging.md)** — `stage.Recipients` partitioned across 32 buckets via modulo-32 persisted computed column, enabling partition-level operations per import without full-table locks. PAGE compression on the temporary-but-large staging data.

- **[SNAPSHOT Isolation Across the Reporting Layer](sql-server-database-engineering-snapshot-isolation.md)** — All reporting procedures use SNAPSHOT isolation for consistent reads against tables actively written by the import pipeline. Visible evolution from READ UNCOMMITTED (early migrations) to SNAPSHOT as the system matured.

- **[Staging-to-Production Pipeline with Temp Table Indexing](sql-server-database-engineering-staging-pipeline.md)** — Five-step pipeline: bulk stage → migrate → scrub (bitflags) → insert → report. Temp table created with four indexes (including columnstore) at declaration time so downstream scrub procedures can rely on them without redundant creation.

- **[Analytical Reporting with Chained CTEs and UTF-8 Encoding](sql-server-database-engineering-analytical-reporting.md)** — Three-CTE chain for per-campaign break-even date computation using `ROWS UNBOUNDED PRECEDING` running totals. Plus a pure T-SQL RFC 3629 UTF-8 encoder using `GENERATE_SERIES` for binary protocol interop.

- **[SQL Server Views as a Stable Read API](sql-analytical-views.md)** — 20+ views across `data` and `reports` schemas forming a composable read API: base join views (import_recipients, MailFileSummary), a five-layer cost aggregation chain, bitflag decomposition via GET_BIT, window-function planning views with DENSE_RANK, and external DTA interface contracts. Views absorb schema migrations so the read path stays stable.
