---
title: SQL Server Database Engineering — Partitioned Staging Tables
tags: [sql-server, partitioning, staging-tables, data-compression, computed-columns, database-design, data-engineering]
related:
  - evidence/sql-server-database-engineering.md
  - evidence/sql-server-database-engineering-staging-pipeline.md
  - evidence/sql-server-database-engineering-snapshot-isolation.md
  - evidence/data-engineering-etl.md
  - evidence/etl-pipeline-framework.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/sql-server-database-engineering.md
---

# SQL Server Database Engineering — Partitioned Staging Tables

The Madera staging layer uses range-partitioned tables to isolate concurrent import batches. The `stage.Recipients` table is partitioned across 32 buckets via a persisted computed column using modulo-32 on the import ID — allowing partition-level truncation and switching for individual imports without locking the entire staging table. PAGE compression reduces storage for the temporary but large (50K-500K rows per import) staging data.

---

## Evidence: Partitioned Staging Tables

The staging layer uses range-partitioned tables to isolate concurrent import batches:

```sql
-- From 20241227/04_stage_Recipients.sql
CREATE PARTITION FUNCTION PF_ImportLog (TINYINT)
AS RANGE RIGHT FOR VALUES (0, 1, 2, 3, ... 31);

CREATE PARTITION SCHEME PS_ImportLog
AS PARTITION PF_ImportLog ALL TO ([PRIMARY]);

CREATE TABLE [stage].[Recipients](
    [RecipientId] [int] IDENTITY(1,1) NOT NULL,
    [ImportLogId] [bigint] NOT NULL,
    [ImportPartition] AS CAST((ABS(ImportLogId) % 32) AS TINYINT) PERSISTED,
    ...
) ON [PS_ImportLog]([ImportPartition]) WITH (DATA_COMPRESSION = PAGE);
```

The `ImportPartition` column is a persisted computed column using modulo-32 on the import ID. This distributes imports across 32 partitions, allowing partition-level operations (truncation, switching) for individual imports without locking the entire staging table. The `DATA_COMPRESSION = PAGE` setting reduces storage for the staging data, which is temporary but can be large (50K-500K rows per import).

## Key Files

- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20241227/04_stage_Recipients.sql` — Partitioned staging table with PAGE compression
