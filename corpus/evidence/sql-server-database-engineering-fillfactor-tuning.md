---
title: SQL Server Database Engineering — FILLFACTOR and OPTIMIZE_FOR_SEQUENTIAL_KEY Tuning
tags: [sql-server, fillfactor, indexing-strategy, performance, optimize-for-sequential-key, sql-server-2022, database-design]
related:
  - evidence/sql-server-database-engineering.md
  - evidence/sql-server-database-engineering-columnstore-indexes.md
  - evidence/sql-server-database-engineering-schema-migrations.md
  - evidence/sql-server-database-engineering-staging-pipeline.md
  - evidence/data-engineering-etl.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/sql-server-database-engineering.md
---

# SQL Server Database Engineering — FILLFACTOR and OPTIMIZE_FOR_SEQUENTIAL_KEY Tuning

The Madera platform's index definitions show deliberate workload-based choices: GUID-keyed tables use low fill factors (50-60%) because inserts scatter randomly across the B-tree, while ascending-key clustered indexes use 95% fill factor with `OPTIMIZE_FOR_SEQUENTIAL_KEY=ON` because new rows always append to the end. The `RecipientId` index on the write-heavy `data.Leads` table uses the most aggressive setting (50%) in the codebase.

---

## Evidence: FILLFACTOR and OPTIMIZE_FOR_SEQUENTIAL_KEY Tuning

The index tuning shows deliberate choices based on workload patterns:

```sql
-- Primary keys: low fill factor for insert-heavy GUID-keyed tables
PRIMARY KEY NONCLUSTERED ([Id]) WITH (FILLFACTOR = 60)

-- Clustered indexes on CreatedOn: high fill factor, sequential key optimization
CREATE CLUSTERED INDEX ix_addresses_createdon ON [data].[Addresses]
    ([CreatedOn] ASC) WITH (OPTIMIZE_FOR_SEQUENTIAL_KEY = ON, FILLFACTOR = 95);

-- Hash lookup index: low fill factor, sort in tempdb
CREATE NONCLUSTERED INDEX ix_addresses_addresshash ON [data].[Addresses]
    ([AddressHash]) INCLUDE ([Id], [AddressFlags])
    WITH (SORT_IN_TEMPDB = ON, FILLFACTOR = 60, PAD_INDEX = ON);
```

The logic: `FILLFACTOR=60` on the nonclustered PK and hash indexes leaves 40% free space per page for new inserts without page splits. These are GUID-keyed, so inserts scatter randomly across the B-tree. The clustered index on `CreatedOn` uses `FILLFACTOR=95` because `OPTIMIZE_FOR_SEQUENTIAL_KEY=ON` (a SQL Server 2019+ feature) handles contention on the last page of the ascending key — new rows always append to the end, so page splits are rare and high fill factor is appropriate.

The `RecipientId` index on `data.Leads` uses `FILLFACTOR=50` — the most aggressive setting in the codebase — because recipient lookups are scattered and the table is write-heavy during import batches.

## Key Files

- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250520/01A_create.sql` — Full index definitions with fill factor and sequential key choices
