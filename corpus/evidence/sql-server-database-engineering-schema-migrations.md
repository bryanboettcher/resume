---
title: SQL Server Database Engineering — Zero-Downtime Schema Migration via Rename Pattern
tags: [sql-server, schema-migrations, zero-downtime, sp-rename, database-design, domain-alignment]
related:
  - evidence/sql-server-database-engineering.md
  - evidence/sql-server-database-engineering-bitflag-architecture.md
  - evidence/sql-server-database-engineering-fillfactor-tuning.md
  - evidence/data-engineering-etl.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/sql-server-database-engineering.md
---

# SQL Server Database Engineering — Zero-Downtime Schema Migration via Rename Pattern

The Madera platform's 26 migration batches (December 2024 through August 2025) evolved the schema continuously under production load. When a large table restructure was needed — changing primary key type, replacing foreign keys, renaming the table — the pattern is create-migrate-drop-rename using `sp_rename` rather than `ALTER TABLE` with column drops and type changes, avoiding long schema locks on large tables.

---

## Evidence: Zero-Downtime Schema Migration via Rename Pattern

The `20250407/01A_importlogs_overhaul.sql` migration performs a production table restructure using a create-migrate-drop-rename pattern:

```sql
-- From 20250407/01A_importlogs_overhaul.sql
CREATE TABLE [data].[Tmp_Leads] (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()),
    ImportId UNIQUEIDENTIFIER NOT NULL,
    RecipientId INT NOT NULL,
    ScrubbedReason INT NOT NULL,
    LeadDate DATE NULL,
    PRIMARY KEY CLUSTERED (Id) WITH (FILLFACTOR=60, OPTIMIZE_FOR_SEQUENTIAL_KEY=ON)
);

INSERT INTO [data].[Tmp_Leads] (ImportId, RecipientId, ScrubbedReason, LeadDate)
SELECT s.CorrelationId AS ImportId, l.RecipientId, l.ScrubbedReason, l.LeadDate
FROM [data].[Leads] l
INNER JOIN [data].[DirectMailImportSagas] s ON l.ImportLogId = s.ImportLogId;

CREATE NONCLUSTERED COLUMNSTORE INDEX ix_leads_importid ON data.Tmp_Leads (ImportId);
CREATE NONCLUSTERED INDEX ix_leads_recipientid ON data.Tmp_Leads (RecipientId);
CREATE NONCLUSTERED INDEX ix_leads_scrubbedreason ON data.Tmp_Leads (ScrubbedReason);

DROP TABLE IF EXISTS [data].[Leads];
EXEC sp_rename 'data.Tmp_Leads', 'Leads';
```

The same migration also renames the saga table to align with evolved domain language:

```sql
EXEC sp_rename 'data.DirectMailImportSagas', 'Imports';
```

This migration does three things simultaneously: restructures the primary key from `BIGINT IDENTITY` to `UNIQUEIDENTIFIER` with `NEWSEQUENTIALID()`, replaces the `ImportLogId` foreign key with a direct `ImportId` GUID correlation, and renames the saga table. The `sp_rename` approach avoids the schema lock duration of `ALTER TABLE` with column drops and type changes on a large table.

The same create-rename pattern appears in `20250520/01A_create.sql`, which renames existing tables to `Tmp_*` before creating new versions:

```sql
EXEC sp_rename 'data.Addresses', 'Tmp_Addresses';
EXEC sp_rename 'data.Recipients', 'Tmp_Recipients';
EXEC sp_rename 'data.Leads', 'Tmp_Leads';
```

## Key Files

- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250407/01A_importlogs_overhaul.sql` — Zero-downtime rename migration with domain alignment
- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250520/01A_create.sql` — Batch rename pattern for major schema revision
