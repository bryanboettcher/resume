---
title: SQL Analytical Views — Base Join Views as Stable Read API
tags: [sql-server, views, denormalization, database-design, direct-mail, stable-api, schema-migration, csharp]
related:
  - evidence/sql-analytical-views.md
  - evidence/sql-analytical-views-aggregation.md
  - evidence/sql-server-database-engineering.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/sql-analytical-views.md
---

# SQL Analytical Views — Base Join Views as Stable Read API

The Madera/Call-Trader platform uses SQL Server views as an abstraction layer between raw normalized tables and the reporting/UI surface. Base join views flatten multi-table joins into a single queryable surface — when the underlying schema changes (table renames, FK type changes), the view absorbs the change and downstream consumers are unaffected.

---

## Evidence: Base Join Views — Stable API Over Normalized Tables

### vw_import_recipients — Lead-to-Import Flattening

This view joins `Leads`, `Recipients`, and `Imports` to present a unified lead record with its import context:

```sql
-- From Madera/Madera.DbMigrations/Migrations/DirectMail/20250407/06A_vw_import_recipients.sql
CREATE OR ALTER VIEW [data].[vw_import_recipients] AS
SELECT
    L.Id AS LeadId,
    L.RecipientId,
    R.AddressId,
    I.VerticalId,
    L.ImportId,
    L.ScrubbedReason,
    I.CostPerLead
FROM data.Leads L
INNER JOIN data.Recipients R ON L.RecipientId = R.Id
INNER JOIN data.Imports I ON I.CorrelationId = L.ImportId;
```

This view is the foundation for most downstream analytics. At least five other views reference `data.vw_import_recipients` rather than joining `Leads`/`Recipients`/`Imports` directly. The schema migrated from `ImportLogs` with integer IDs to `Imports` with GUID `CorrelationId` foreign keys, and this view absorbed that change — consumers didn't need updating.

### vw_MailFileSummary — Denormalized Mail File with Reference Data

The mail file summary view joins `MailFiles` with four reference tables to present a denormalized record with human-readable names:

```sql
-- From Madera/Madera.Workflows/Migrations/DirectMail/20250407/14A_vw_MailFileSummary.sql
ALTER VIEW [data].[vw_MailFileSummary] AS
SELECT
    CorrelationId, CurrentState, Filename,
    MailHouseId, m.Name AS MailHouseName,
    CreatedOn, PopulationStartedOn, LastUpdatedOn,
    PopulationCompletedOn, FinalizedOn, MailDate,
    TotalRecipients, LastStatus, ExceptionMessage,
    mf.BrokerId, mf.PublisherId, VerticalId,
    b.Name AS BrokerName, p.Name AS PublisherName,
    v.Name AS VerticalName, InboundPhoneNumber
FROM [data].[MailFiles] mf
INNER JOIN [ref].[Brokers] b ON mf.BrokerId = b.Id
INNER JOIN [ref].[Publishers] p ON mf.PublisherId = p.Id
INNER JOIN [ref].[Verticals] v ON mf.VerticalId = v.Id
INNER JOIN [ref].[MailHouses] m ON mf.MailHouseId = m.Id;
```

The underlying table changed from `DirectMailFileSagas` to `MailFiles` during the April 2025 rename migration. The view absorbed that structural change, providing a stable read contract for the UI. The C# `MailPlanningSummaryService` queries the materialized `reports.MailPlanning` table populated from views like this one — the service never touches the base tables.

### vw_mailed_recipients — Export-Ready Recipient Address Data

A simple three-table join producing the exact column set needed for mail house export files:

```sql
-- From Madera/Madera.Workflows/Migrations/DirectMail/20250228/01A_vw_mailed_recipients.sql
CREATE OR ALTER VIEW data.vw_mailed_recipients AS
SELECT
    mr.MailFileId,
    r.FirstName, r.LastName,
    a.Address1, a.Address2, a.City, a.State, a.Zip, a.ZipPlus4
FROM data.MailRecipients mr
INNER JOIN data.Recipients r ON r.Id = mr.RecipientId
INNER JOIN data.Addresses a ON r.AddressId = a.Id;
```

### vw_dtaRecipients / vw_dtaRecipientsQueue — External System Interface Views

Two views shape recipient data into the exact column layout expected by an external DTA (Data Transfer Agent) system. These include computed defaults (`NULL AS lastMailDate`, `0 AS timesMailed`, `1 AS isDeliverable`) and column aliasing to match the external contract:

```sql
-- From Madera/Madera.DbMigrations/Migrations/DirectMail/20241227/07_vw_dtaRecipients.sql
CREATE OR ALTER VIEW data.vw_dtaRecipients AS
SELECT
    R.RecipientId AS ID,
    R.FirstName AS firstName,
    ...
    NULL AS lastMailDate,
    0 AS timesMailed,
    1 AS isDeliverable,
    1 AS isValid,
    ...
    P.FreeRemail AS remailAllowed,
    1 AS isActive
FROM data.Recipients R
INNER JOIN data.Addresses A ON R.AddressId = A.Id
INNER JOIN data.ImportLogs I ON R.ImportLogId = I.Id
INNER JOIN ref.BrokerPublisherLink BPL ON I.BrokerPublisherLinkId = BPL.Id
INNER JOIN ref.Publishers P ON BPL.PublisherId = P.Id;
```

The queue variant (`vw_dtaRecipientsQueue`) adds `NEWID() AS ID` for generating unique identifiers per queue message and includes additional null placeholder columns (`oAddress1`, `oAddress2`, `oZip`, `oCity`, `oState`) matching the external schema. These views act as a contract boundary — the internal schema evolves freely while the DTA interface stays fixed.

---

## Key Files

- `madera-apps:Madera/Madera.DbMigrations/Migrations/DirectMail/20250407/06A_vw_import_recipients.sql` — Base join view: Leads + Recipients + Imports
- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250407/14A_vw_MailFileSummary.sql` — Denormalized mail file with reference data joins
- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250228/01A_vw_mailed_recipients.sql` — Export-ready recipient addresses
- `madera-apps:Madera/Madera.DbMigrations/Migrations/DirectMail/20241227/07_vw_dtaRecipients.sql` — External system interface contract
