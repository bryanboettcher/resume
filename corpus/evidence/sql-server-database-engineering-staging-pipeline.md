---
title: SQL Server Database Engineering — Staging-to-Production Pipeline with Temp Table Indexing
tags: [sql-server, staging-pipeline, temp-tables, indexing-strategy, sqlbulkcopy, stored-procedures, data-engineering, scrubbing]
related:
  - evidence/sql-server-database-engineering.md
  - evidence/sql-server-database-engineering-bitflag-architecture.md
  - evidence/sql-server-database-engineering-columnstore-indexes.md
  - evidence/sql-server-database-engineering-partitioned-staging.md
  - evidence/sql-server-database-engineering-snapshot-isolation.md
  - evidence/data-engineering-etl.md
  - evidence/etl-pipeline-framework.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/sql-server-database-engineering.md
---

# SQL Server Database Engineering — Staging-to-Production Pipeline with Temp Table Indexing

The Madera stored procedures follow a five-step pattern for moving data from staging to production to reporting. A critical detail: the `#import_recipients` temp table is created with four indexes at declaration time (including a columnstore) and then shared across multiple scrub sub-procedures — each scrub procedure relies on those pre-existing indexes for its specific access pattern, avoiding redundant index creation per procedure.

---

## Evidence: Staging-to-Production Pipeline with Temp Table Indexing

The stored procedures follow a consistent pattern for moving data from staging to production to reporting:

1. **Stage:** Bulk-insert raw data into partitioned `stage.Recipients` (done by the C# pipeline via SqlBulkCopy)
2. **Migrate:** `usp_MigrateRecipients` reads from staging, joins to `data.Addresses` via hash, creates an indexed temp table, delegates to scrub procedures
3. **Scrub:** Individual `usp_scrub_*` procedures operate on the shared `#import_recipients` temp table, setting bits on `ScrubbedReason`
4. **Insert:** `usp_ScrubRecipients` inserts the scrubbed results into `data.Recipients` and drops the temp table
5. **Report:** `usp_Update*` procedures read from production tables into `#temp`, truncate or delete from report tables, then insert

The temp tables are indexed at creation time for downstream performance:

```sql
-- usp_MigrateRecipients creates the temp table with 4 indexes
INDEX ix_temp_importid NONCLUSTERED COLUMNSTORE ([ImportId]),
INDEX ix_temp_lookups NONCLUSTERED ([AddressId], [VerticalId]),
INDEX ix_temp_addressflags NONCLUSTERED ([AddressFlags]),
INDEX ix_temp_scrubbedreason NONCLUSTERED ([ScrubbedReason])
```

The scrub procedures then rely on these indexes — `usp_scrub_ExternalDuplicates` creates its own `#address_temp` with a nonclustered index for the `IN` subquery, while `usp_scrub_InternalDuplicates` uses a windowed `ROW_NUMBER()` partitioned by `AddressId` (served by `ix_temp_lookups`).

## Key Files

- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250520/02A_usp_MigrateRecipients.sql` — Staging pipeline with columnstore temp tables and SNAPSHOT isolation
- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250520/02A_usp_scrub_ExternalDuplicates.sql` — SET_BIT bitflag scrubbing with indexed temp table joins
- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250520/02A_usp_scrub_InternalDuplicates.sql` — Windowed ROW_NUMBER dedup with bitflag tracking
