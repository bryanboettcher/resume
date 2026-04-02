---
title: SQL Server Database Engineering — Columnstore Indexes on OLTP Data
tags: [sql-server, columnstore-indexes, oltp-olap, indexing-strategy, batch-mode, performance, sql-server-2022]
related:
  - evidence/sql-server-database-engineering.md
  - evidence/sql-server-database-engineering-bitflag-architecture.md
  - evidence/sql-server-database-engineering-staging-pipeline.md
  - evidence/sql-server-database-engineering-fillfactor-tuning.md
  - evidence/data-engineering-etl.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/sql-server-database-engineering.md
---

# SQL Server Database Engineering — Columnstore Indexes on OLTP Data

The Madera/Call-Trader direct mail platform applies a hybrid OLTP/OLAP indexing pattern on the `data.Leads` table: traditional row-store indexes support the transactional write path while a nonclustered columnstore index on `ImportId` enables batch-mode execution for analytical reporting queries. The same pattern is applied to temp tables within stored procedures, which is unusual but justified by the shared-temp-table design of the scrub pipeline.

---

## Evidence: Columnstore Indexes on OLTP Data

The `data.Leads` table carries a nonclustered columnstore index alongside traditional row-store indexes:

```sql
-- From 20250520/01A_create.sql
CREATE NONCLUSTERED COLUMNSTORE INDEX ix_leads_importid ON [data].[Leads] (ImportId);
CREATE NONCLUSTERED INDEX ix_leads_scrubbedreason ON [data].[Leads] (ScrubbedReason)
    WITH (SORT_IN_TEMPDB = ON, FILLFACTOR = 90, PAD_INDEX = ON);
CREATE NONCLUSTERED INDEX ix_leads_recipientid ON [data].[Leads] (RecipientId)
    WITH (SORT_IN_TEMPDB = ON, FILLFACTOR = 50, PAD_INDEX = OFF);
```

This is a hybrid OLTP/OLAP pattern: the row-store indexes support the transactional write path (individual lead inserts, scrub lookups), while the columnstore index on `ImportId` supports the analytical reporting queries that aggregate across entire imports. SQL Server's batch-mode execution on the columnstore path gives the reporting procedures (e.g., `usp_ImportLogReport`, `usp_MailPlanningReport`) columnar scan performance without requiring a separate analytical database.

The same pattern appears on temp tables within stored procedures:

```sql
-- From usp_MigrateRecipients (20250520)
CREATE TABLE #import_recipients (
    ...
    INDEX ix_temp_importid NONCLUSTERED COLUMNSTORE ([ImportId]),
    INDEX ix_temp_lookups NONCLUSTERED ([AddressId], [VerticalId]),
    INDEX ix_temp_addressflags NONCLUSTERED ([AddressFlags]),
    INDEX ix_temp_scrubbedreason NONCLUSTERED ([ScrubbedReason])
);
```

Putting a columnstore index on a temp table is unusual. It makes sense here because `#import_recipients` is shared across multiple scrub sub-procedures (passed via temp table scope from `usp_MigrateRecipients` → `usp_ScrubRecipients` → individual `usp_scrub_*` procedures), and some downstream queries benefit from batch-mode execution on the `ImportId` grouping.

## Key Files

- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250520/01A_create.sql` — Columnstore indexes on data.Leads alongside row-store indexes
- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250520/02A_usp_MigrateRecipients.sql` — Staging pipeline with columnstore temp tables
