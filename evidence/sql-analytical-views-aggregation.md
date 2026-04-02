---
title: SQL Analytical Views — Composable Cost and Count Aggregation Chain
tags: [sql-server, views, aggregation, cost-tracking, composable-views, business-rules, direct-mail, analytical-queries]
related:
  - evidence/sql-analytical-views.md
  - evidence/sql-analytical-views-base-join-views.md
  - evidence/sql-analytical-views-reporting.md
  - evidence/sql-server-database-engineering.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/sql-analytical-views.md
---

# SQL Analytical Views — Composable Cost and Count Aggregation Chain

The Madera cost tracking system uses a chain of five views where each builds on the previous, forming a composable aggregation pipeline from raw recipient counts to total campaign cost. Each layer has a single responsibility and is independently queryable for validation.

---

## Evidence: Aggregation Views — Composable Cost and Count Analytics

### View Composition Chain: Counts → Costs → File-Level Totals

**Layer 1 — `vw_mailfile_counts`:** Raw recipient count per mail file.

```sql
-- From 20250214/02A_vw_mailfile_counts.sql
CREATE OR ALTER VIEW data.vw_mailfile_counts AS
SELECT MailFileId, COUNT(*) AS TotalRecipients
FROM data.MailRecipients
GROUP BY MailFileId;
```

**Layer 2 — `vw_mailfile_stats`:** Multiplies per-recipient mail cost by count to get total mail cost.

```sql
-- From 20250214/04A_vw_mailfile_stats.sql
CREATE OR ALTER VIEW [data].[vw_mailfile_stats] AS
SELECT MailFileId, MailCost * TotalRecipients AS TotalMailCost
FROM data.MailFiles F
INNER JOIN [data].[vw_mailfile_counts] C on C.MailFileID = F.Id;
```

**Layer 3 — `vw_recipient_lead_cost`:** Minimum cost-per-lead for each recipient (taking the cheapest import source).

```sql
-- From 20250214/06A_vw_recipient_lead_cost.sql
CREATE OR ALTER VIEW data.vw_recipient_lead_cost AS
SELECT RecipientId, MIN(CostPerLead) AS LeadCost
FROM [data].[vw_import_recipients] V
WHERE V.ScrubbedReason = 0
GROUP BY RecipientId;
```

**Layer 4 — `vw_mail_recipient_cost`:** Business rule — first-time-mailed recipients have zero lead cost (the mail itself is the cost); re-mailed recipients carry their lead acquisition cost.

```sql
-- From 20250214/07A_vw_mail_recipient_cost.sql
CREATE OR ALTER VIEW data.vw_mail_recipient_cost AS
SELECT rms.RecipientId, IIF(TimesMailed = 1, 0, LeadCost) AS LeadCost
FROM data.vw_recipient_mail_summary rms
INNER JOIN data.vw_recipient_lead_cost rlc ON rms.RecipientId = rlc.RecipientId;
```

**Layer 5 — `vw_mailfile_lead_cost`:** Aggregates per-recipient lead costs up to the mail file level.

```sql
-- From 20250214/08A_vw_mailfile_lead_cost.sql
CREATE OR ALTER VIEW data.vw_mailfile_lead_cost AS
SELECT MailFileId, SUM(LeadCost) AS TotalLeadCost
FROM data.MailRecipients mr
INNER JOIN data.vw_mail_recipient_cost mrc ON mr.RecipientId = mrc.RecipientId
GROUP BY MailFileId;
```

This five-layer chain computes total campaign cost from raw data without any intermediate materialization. Each layer has a single responsibility and is independently testable. The `IIF(TimesMailed = 1, 0, LeadCost)` rule in layer 4 encodes a business decision: re-mails carry the data cost because the lead was already purchased, but first-mailers don't because the lead cost is sunk into the initial campaign.

---

## Key Files

- `madera-apps:Madera/Madera.DbMigrations/Migrations/DirectMail/20250214/02A_vw_mailfile_counts.sql` — Layer 1 of cost chain
- `madera-apps:Madera/Madera.DbMigrations/Migrations/DirectMail/20250214/07A_vw_mail_recipient_cost.sql` — Business rule: first-mail vs re-mail cost attribution
- `madera-apps:Madera/Madera.DbMigrations/Migrations/DirectMail/20250214/08A_vw_mailfile_lead_cost.sql` — Aggregated file-level lead cost
