---
title: SQL Server Database Engineering — Bitflag Architecture for Address and Scrub Metadata
tags: [sql-server, bitflags, get-bit, set-bit, computed-columns, sql-server-2022, schema-design, data-engineering, direct-mail]
related:
  - evidence/sql-server-database-engineering.md
  - evidence/sql-server-database-engineering-columnstore-indexes.md
  - evidence/sql-server-database-engineering-staging-pipeline.md
  - evidence/data-engineering-etl.md
  - evidence/etl-pipeline-framework.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/sql-server-database-engineering.md
---

# SQL Server Database Engineering — Bitflag Architecture for Address and Scrub Metadata

The Madera/Call-Trader platform processes 30M+ recipient records through a SQL Server 2022 database. Address verification results and scrub rejection reasons are each stored as a single `INT` bitflag column and decomposed into queryable boolean dimensions via SQL Server 2022's `GET_BIT`/`SET_BIT` functions — packing 17 USPS verification dimensions and 20+ scrub reasons into 4 bytes per row while maintaining a clean query surface through computed columns.

---

## Evidence: Bitflag Architecture for Address and Scrub Metadata

### Address Flags — 17 Computed Columns via GET_BIT

The `data.Addresses` table stores USPS-certified address verification results as a single `INT` column (`AddressFlags`) with 17 computed columns that decompose individual bits into queryable boolean fields:

```sql
-- From Madera/Madera.Workflows/Migrations/DirectMail/20250520/01A_create.sql
ZipCode_IsStandard      AS GET_BIT(AddressFlags, 0),
ZipCode_IsPoBox         AS GET_BIT(AddressFlags, 1),
ZipCode_IsUnique        AS GET_BIT(AddressFlags, 2),
ZipCode_IsMilitary      AS GET_BIT(AddressFlags, 3),
Address_IsResidential   AS GET_BIT(AddressFlags, 4),
Address_IsCommercial    AS GET_BIT(AddressFlags, 5),
Address_IsValid         AS GET_BIT(AddressFlags, 6),
Address_IsDeliverable   AS GET_BIT(AddressFlags, 7),
Deliverability_IsDeliverable       AS GET_BIT(AddressFlags, 8),
Deliverability_RemoveSecondary     AS GET_BIT(AddressFlags, 9),
Deliverability_IncorrectSecondary  AS GET_BIT(AddressFlags, 10),
Deliverability_MissingSecondary    AS GET_BIT(AddressFlags, 11),
Deliverability_UspsUndeliverable   AS GET_BIT(AddressFlags, 12),
Deliverability_IsVacant            AS GET_BIT(AddressFlags, 13),
Deliverability_IsInformed          AS GET_BIT(AddressFlags, 14),
Deliverability_IsPhantom           AS GET_BIT(AddressFlags, 15),
Deliverability_IsGeneralDelivery   AS GET_BIT(AddressFlags, 16),
```

This design packs 17 USPS verification dimensions into 4 bytes per row. At 10-15M addresses, this saves roughly 200MB compared to storing each flag as a separate `BIT` column (which SQL Server packs 8-per-byte but still requires per-column overhead in metadata, indexes, and page layout). The computed columns provide a clean query surface without materializing the storage cost.

### Scrub Reason Flags — SET_BIT for Multi-Reason Tracking

The scrub pipeline uses a parallel bitflag pattern on `ScrubbedReason`, where each scrub procedure sets its own bit position using `SET_BIT`. A reference table (`ref.ScrubbedReasons`) documents the bit layout with explicit `BitPosition` and `BitValue` columns:

```sql
-- From Madera/Madera.Workflows/Migrations/DirectMail/20250130/01B_ref_ScrubbedReasons.sql
INSERT INTO [ref].[ScrubbedReasons] (BitPosition, BitValue, Description) VALUES
    ( 0, 1 <<  0, 'Recipient Underage'),
    ( 4, 1 <<  4, 'Profanity detected'),
    (16, 1 << 16, 'Address is General Delivery'),
    (17, 1 << 17, 'Address is PO Box'),
    (18, 1 << 18, 'Address is invalid'),
    (19, 1 << 19, 'Lead is an internal duplicate'),
    (20, 1 << 20, 'Lead is an external duplicate'),
```

The gap between bits 4 and 16 separates recipient-level scrub reasons (bits 0-4: underage, invalid DOB, out-of-range DOB, missing name, profanity) from address-level reasons (bits 16+). Each scrub sub-procedure sets exactly one bit, so a single recipient can accumulate multiple independent rejection reasons without any overwriting:

```sql
-- From usp_scrub_ExternalDuplicates (20250520)
UPDATE t SET t.ScrubbedReason = SET_BIT(t.ScrubbedReason, 20)
FROM #import_recipients t WHERE t.AddressId IN (SELECT Id FROM #address_temp);

-- From usp_scrub_InternalDuplicates (20250520)
WITH ranked AS (
    SELECT Id, AddressId, ScrubbedReason,
           ROW_NUMBER() OVER (PARTITION BY AddressId ORDER BY CreatedOn ASC) as RowNum
    FROM #import_recipients
)
UPDATE ranked SET ScrubbedReason = SET_BIT(ScrubbedReason, 19) WHERE RowNum > 1;

-- From usp_scrub_AddressInvalid (20250520)
UPDATE #import_recipients SET ScrubbedReason = SET_BIT(ScrubbedReason, 18)
WHERE AddressId = CAST(0x0 AS UNIQUEIDENTIFIER);
```

The reporting layer then reads these flags back with `GET_BIT` to decompose each reason into separate aggregate counts:

```sql
-- From usp_ImportLogReport (20250206)
SUM(CAST(GET_BIT(V.PriorityScrubbedReason,  0) AS INT)) AS TotalRecipientUnderage,
SUM(CAST(GET_BIT(V.PriorityScrubbedReason,  4) AS INT)) AS TotalContainsBadWord,
SUM(CAST(GET_BIT(V.PriorityScrubbedReason, 17) AS INT)) AS TotalAddressPoBox,
SUM(CAST(GET_BIT(V.PriorityScrubbedReason, 19) AS INT)) AS TotalInternalDuplicate,
SUM(CAST(GET_BIT(V.PriorityScrubbedReason, 20) AS INT)) AS TotalExternalDuplicate
```

The `GET_BIT` and `SET_BIT` functions are SQL Server 2022 additions. Using them instead of manual `& (1 << N)` bitmask arithmetic makes the intent explicit and the code auditable — important when the bit layout encodes TCPA compliance decisions.

## Key Files

- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250520/01A_create.sql` — Core table definitions with bitflag computed columns
- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250130/01B_ref_ScrubbedReasons.sql` — Scrub reason bit layout reference table
- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250206/03A_usp_ImportLogReport.sql` — GET_BIT aggregation for scrub reason reporting
- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250520/02A_usp_scrub_ExternalDuplicates.sql` — SET_BIT bitflag scrubbing with indexed temp table joins
- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250520/02A_usp_scrub_InternalDuplicates.sql` — Windowed ROW_NUMBER dedup with bitflag tracking
