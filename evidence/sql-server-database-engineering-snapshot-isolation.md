---
title: SQL Server Database Engineering — SNAPSHOT Isolation Across the Reporting Layer
tags: [sql-server, snapshot-isolation, reporting, concurrency, transaction-isolation, database-design, performance]
related:
  - evidence/sql-server-database-engineering.md
  - evidence/sql-server-database-engineering-partitioned-staging.md
  - evidence/sql-server-database-engineering-staging-pipeline.md
  - evidence/sql-server-database-engineering-analytical-reporting.md
  - evidence/data-engineering-etl.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/sql-server-database-engineering.md
---

# SQL Server Database Engineering — SNAPSHOT Isolation Across the Reporting Layer

Every reporting stored procedure in the Madera platform uses `SET TRANSACTION ISOLATION LEVEL SNAPSHOT`, giving each procedure a consistent point-in-time view of tables actively written by the import pipeline — without acquiring shared locks that would block concurrent imports. The evolution from `READ UNCOMMITTED` (early migrations) to `SNAPSHOT` (mature system) is visible in the migration history.

---

## Evidence: SNAPSHOT Isolation Across the Reporting Layer

Every reporting stored procedure in the codebase uses `SET TRANSACTION ISOLATION LEVEL SNAPSHOT`:

- `usp_UpdateImports` — import status reporting
- `usp_UpdateMailFiles` — mail file status reporting
- `usp_UpdateMailFilePerformance` — revenue and break-even analysis
- `usp_UpdateMailRecipients` — recipient data for mail files
- `usp_scrub_ExternalDuplicates` — cross-batch duplicate detection
- `usp_scrub_InternalDuplicates` — within-batch duplicate detection

This is a deliberate architectural choice: the reporting procedures read from transactional tables (`data.Leads`, `data.Imports`, `data.Recipients`) that are actively written to during import processing. SNAPSHOT isolation gives each reporting procedure a consistent point-in-time view without acquiring shared locks that would block the import pipeline. The trade-off is tempdb pressure from row versioning, but the queries are bounded (single-import or full-refresh) and the temp table materialization pattern limits the version store lifetime.

The evolution is visible across migration batches. The earlier `usp_MigrateRecipients` (20250326) used `READ UNCOMMITTED` for speed during staging reads, but by 20250520 the same procedure was upgraded to `SNAPSHOT` — trading raw staging speed for consistency guarantees as the system matured.

## Key Files

- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250520/02A_usp_MigrateRecipients.sql` — Upgraded to SNAPSHOT isolation
- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250523/05A_usp_updateMailFilePerformance.sql` — Reporting procedure with SNAPSHOT isolation
