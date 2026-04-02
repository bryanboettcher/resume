---
title: SQL Analytical Views — Reporting Views and Dashboard Analytics
tags: [sql-server, views, reporting, dashboard, window-functions, dense-rank, materialized-tables, analytical-queries, direct-mail]
related:
  - evidence/sql-analytical-views.md
  - evidence/sql-analytical-views-base-join-views.md
  - evidence/sql-analytical-views-aggregation.md
  - evidence/sql-analytical-views-bitflag-window.md
  - evidence/sql-server-database-engineering.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/sql-analytical-views.md
---

# SQL Analytical Views — Reporting Views and Dashboard Analytics

The `reports` schema in Madera contains dashboard-ready views that compose `data` schema views into analytics-ready datasets. The most complex is `vw_recipients_mail_planning`, which feeds a stored procedure that materializes aggregated results into a table for the C# reporting service.

---

## Evidence: Reporting Views — Composing Views for Dashboard Analytics

### vw_timesmailed_publisher — Publisher-Level Mail Frequency Report

This reporting view composes two data-layer views (`vw_recipient_publisher` and `vw_recipient_mail_summary`) to produce publisher-level mail frequency counts:

```sql
-- From 20250214/12A_vw_timesmailed_publisher.sql
CREATE OR ALTER VIEW reports.vw_timesmailed_publisher AS
SELECT
    ms.LastMailDate AS MailDate,
    ms.TimesMailed,
    p.Name AS PublisherName,
    COUNT(*) AS TotalRecipients
FROM data.vw_recipient_publisher rp
INNER JOIN data.vw_recipient_mail_summary ms ON ms.RecipientId = rp.RecipientId
INNER JOIN ref.Publishers p ON p.Id = rp.PublisherId
GROUP BY ms.LastMailDate, ms.TimesMailed, p.Name;
```

This sits in the `reports` schema, distinct from the `data` schema views it composes. The schema separation enforces the read API boundary: `data.*` views are building blocks; `reports.*` views are dashboard-ready.

### vw_recipients_mail_planning — Full-Context Planning View

The mail planning view is the most complex, joining five tables with a `DENSE_RANK` window function for import ordering:

```sql
-- From 20250624/03A_vw_recipients_mail_planning.sql
CREATE OR ALTER VIEW data.vw_recipients_mail_planning AS
SELECT
    L.RecipientId, R.AddressId, A.State AS StateName,
    I.VerticalId, I.BrokerId, I.PublisherId,
    I.CorrelationId AS ImportId,
    CAST(I.CreatedOn AS DATE) AS ImportDate,
    I.Filename AS ImportFileName,
    DENSE_RANK() OVER (
        PARTITION BY L.RecipientId, I.VerticalId
        ORDER BY I.CreatedOn, L.Id
    ) AS ImportOrder,
    MF.CorrelationId AS MailFileId,
    MF.MailDate,
    MF.Filename AS MailFileName
FROM data.Leads L
INNER JOIN data.Recipients R ON L.RecipientId = R.Id
INNER JOIN data.Addresses A ON R.AddressId = A.Id
INNER JOIN data.Imports I ON L.ImportId = I.CorrelationId
LEFT JOIN data.MailRecipients MR ON R.Id = MR.RecipientId
LEFT JOIN data.MailFiles MF ON MR.MailFileId = MF.CorrelationId
    AND MF.VerticalId = I.VerticalId
WHERE L.ScrubbedReason = 0
```

This view feeds `usp_MailPlanningReport`, a stored procedure that materializes aggregated results into the `reports.MailPlanning` table. The procedure uses a CTE to first aggregate per-recipient mail counts from this view (filtering to `ImportOrder = 1` to get the original import only), then groups by vertical/broker/publisher/state/times-mailed with a special business rule: for the "Direct" broker, publishers are collapsed to "All publishers" when `TimesMailed > 0`. The `MailPlanningSummaryService` in C# then queries the materialized table with EF Core LINQ, applying additional filters and re-aggregation for the UI.

### vw_available_import_reports — Self-Describing Report Registry

```sql
-- From 20250206/04A_reports_vw_available_import_reports.sql
CREATE OR ALTER VIEW [reports].[vw_available_import_reports] AS
(
    SELECT [Id], [ProcedureName]
    FROM [reports].[PostImportProcedures]
    WHERE IsEnabled = 1
);
```

This is a metadata view — it exposes which post-import report procedures are currently enabled, allowing the application to dynamically discover available reports without hardcoding procedure names. The `IsEnabled` flag means reports can be toggled without code deployment.

---

## Key Files

- `madera-apps:Madera/Madera.DbMigrations/Migrations/DirectMail/20250624/03A_vw_recipients_mail_planning.sql` — Complex planning view with window functions
- `madera-apps:Madera/Madera.DbMigrations/Migrations/DirectMail/20250624/04A_usp_MailPlanningReport.sql` — Stored procedure consuming the planning view
- `madera-apps:Madera/Madera.UI.Server/Services/MailPlanningReportService.cs` — C# EF Core service consuming materialized report data
