---
title: SQL Server Views as a Stable Read API for Reporting and Analytics
tags: [sql-server, views, aggregation, reporting, denormalization, database-design, direct-mail, analytical-queries, window-functions]
related:
  - evidence/sql-server-database-engineering.md
  - evidence/sql-server-database-engineering-analytical-reporting.md
  - evidence/sql-server-database-engineering-bitflag-architecture.md
  - evidence/data-engineering-etl.md
  - projects/call-trader-madera.md
children:
  - evidence/sql-analytical-views-base-join-views.md
  - evidence/sql-analytical-views-aggregation.md
  - evidence/sql-analytical-views-bitflag-window.md
  - evidence/sql-analytical-views-reporting.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/sql-server-database-engineering.md
---

# SQL Server Views as a Stable Read API for Reporting and Analytics — Index

The Madera/Call-Trader platform uses SQL Server views as an abstraction layer between raw normalized tables and the reporting/UI surface. Over 20 named views span four categories: base join views that flatten normalized relationships, aggregation views that pre-compute counts and costs, bitflag decomposition views for scrub analysis, and reporting views that compose other views into analytics-ready datasets. These views evolved across 8+ migration batches (December 2024 through June 2025), with `CREATE OR ALTER VIEW` enabling non-breaking iteration. The C# application layer (EF Core, Dapper) queries views rather than base tables, meaning the underlying schema can change without breaking the read path.

### Design Principles

**Schema separation as access control.** The `data` schema contains base views that flatten joins and compute intermediate aggregations. The `reports` schema contains dashboard-ready views that compose `data` views.

**View composition over duplication.** The five-layer cost calculation chain (`vw_mailfile_counts` → `vw_mailfile_stats` → `vw_recipient_lead_cost` → `vw_mail_recipient_cost` → `vw_mailfile_lead_cost`) builds layers where each has one job. Any intermediate layer can be queried directly to validate the numbers.

**Views absorb schema migrations.** The `vw_import_recipients` view changed from joining `ImportLogs` (integer FK) to `Imports` (GUID CorrelationId FK) without breaking any of the five views that reference it. The `vw_MailFileSummary` view changed its source table from `DirectMailFileSagas` to `MailFiles` during the April 2025 rename migration. In both cases, downstream consumers were unaffected.

**`CREATE OR ALTER VIEW` for safe iteration.** Every view uses `CREATE OR ALTER`, which is idempotent — it either creates or updates the view definition without requiring existence checks. This makes the migration scripts re-runnable and the deployment order less fragile.

**Business rules encoded in views, not application code.** The `IIF(TimesMailed = 1, 0, LeadCost)` rule in `vw_mail_recipient_cost` is a business decision about cost attribution. The address-level grouping in `vw_recipient_mail_summary` prevents duplicate mailings to the same physical address. The `ScrubbedReason = (1 << 20)` filter in `vw_multi_leads` identifies re-purchased leads. These rules live in the database where they apply uniformly regardless of which application queries the data.

The full evidence is split into focused documents:

## Child Documents

- **[Base Join Views as Stable Read API](sql-analytical-views-base-join-views.md)** — `vw_import_recipients` (foundation for five downstream views), `vw_MailFileSummary` (four-way reference data join), `vw_mailed_recipients` (export-ready address columns), and the `vw_dtaRecipients`/`vw_dtaRecipientsQueue` external system interface views with computed null placeholders.

- **[Composable Cost and Count Aggregation Chain](sql-analytical-views-aggregation.md)** — The five-layer view chain from raw `MailRecipients` counts to total campaign lead cost, including the `IIF(TimesMailed = 1, 0, LeadCost)` business rule for first-mail vs re-mail cost attribution.

- **[Bitflag Decomposition and Window Functions](sql-analytical-views-bitflag-window.md)** — `vw_leads_scrub` using `GET_BIT` to unpack 10 scrub reason bits into boolean columns plus `ufn_GetPriorityScrub` for single-reason reporting. `vw_recipient_mail_summary` with `COUNT(DISTINCT)` and `LEFT JOIN` for zero-mailing addresses. `vw_multi_leads` with `DENSE_RANK()` for import ordering of re-purchased leads.

- **[Reporting Views and Dashboard Analytics](sql-analytical-views-reporting.md)** — `vw_timesmailed_publisher` composing data-layer views into publisher-level frequency counts. `vw_recipients_mail_planning` (the most complex view) with `DENSE_RANK` import ordering, feeding `usp_MailPlanningReport` which materializes into `reports.MailPlanning` for EF Core consumption. `vw_available_import_reports` as a self-describing metadata registry.
