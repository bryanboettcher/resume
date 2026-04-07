---
title: SQL Analytical Views — Bitflag Decomposition and Window Functions
tags: [sql-server, views, bitflag, window-functions, dense-rank, get-bit, analytical-queries, direct-mail, scrub-analysis]
related:
  - evidence/sql-analytical-views.md
  - evidence/sql-analytical-views-base-join-views.md
  - evidence/sql-analytical-views-aggregation.md
  - evidence/sql-analytical-views-reporting.md
  - evidence/sql-server-database-engineering-bitflag-architecture.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/sql-analytical-views.md
---

# SQL Analytical Views — Bitflag Decomposition and Window Functions

Two views in the Madera platform use advanced SQL techniques for analytics: `vw_leads_scrub` decomposes a bitflag-encoded column into individual boolean dimensions using `GET_BIT`, and `vw_recipient_mail_summary` uses `COUNT(DISTINCT)` with `LEFT JOIN` to compute per-address mailing frequency, including addresses with zero mailings.

---

## Evidence: Scrub Decomposition View — Bitflag Unpacking for Analytics

The `vw_leads_scrub` view decomposes the bitflag-encoded `ScrubbedReason` column into individual boolean dimensions, using a UDF for priority determination:

```sql
-- From 20250407/06D_vw_leads_scrub.sql
CREATE OR ALTER VIEW [data].[vw_leads_scrub] AS
SELECT
    Id as [LeadId],
    [ImportId],
    [ref].[ufn_GetPriorityScrub]([ScrubbedReason]) AS [PriorityScrubbedReason],
    IIF([ScrubbedReason] > 0, 1, 0) AS [IsScrubbed],
    CAST(GET_BIT([ScrubbedReason], 0) AS INT) AS [IsRecipientUnderage],
    CAST(GET_BIT([ScrubbedReason], 1) AS INT) AS [IsDobInvalid],
    CAST(GET_BIT([ScrubbedReason], 2) AS INT) AS [IsDobOutOfRange],
    CAST(GET_BIT([ScrubbedReason], 3) AS INT) AS [IsMissingName],
    CAST(GET_BIT([ScrubbedReason], 4) AS INT) AS [ContainsBadWord],
    CAST(GET_BIT([ScrubbedReason], 16) AS INT) AS [IsAddressGeneralDelivery],
    CAST(GET_BIT([ScrubbedReason], 17) AS INT) AS [IsAddressPoBox],
    CAST(GET_BIT([ScrubbedReason], 18) AS INT) AS [IsInvalidAddress],
    CAST(GET_BIT([ScrubbedReason], 19) AS INT) AS [IsInternalDuplicate],
    CAST(GET_BIT([ScrubbedReason], 20) AS INT) AS [IsExternalDuplicate]
FROM [data].[Leads];
```

This is the read-path counterpart to the `SET_BIT` writes documented in the bitflag architecture evidence. The `ufn_GetPriorityScrub` UDF determines which scrub reason takes precedence when multiple bits are set — necessary because downstream reports need a single "primary reason" per rejected lead. The gap between bits 4 and 16 reflects the separation between recipient-level reasons (underage, bad DOB, missing name, profanity) and address-level reasons (general delivery, PO box, invalid, duplicates).

---

## Evidence: Mail Summary View with Window Functions

The `vw_recipient_mail_summary` view aggregates mailing history per address+vertical combination, using `COUNT(DISTINCT)` to handle recipients who appear in multiple mail files:

```sql
-- From 20250509/04A_vw_recipient_mail_summary.sql
ALTER VIEW data.vw_recipient_mail_summary AS
SELECT
    A.Id AS AddressId,
    I.VerticalId,
    COUNT(DISTINCT MF.CorrelationId) AS TimesMailed,
    MIN(R.Id) AS RecipientId,
    MAX(MF.MailDate) AS LastMailDate
FROM data.Addresses A
JOIN data.Recipients R ON A.Id = R.AddressId
JOIN data.Leads L ON L.RecipientId = R.Id
JOIN data.Imports I ON I.CorrelationId = L.ImportId
LEFT JOIN data.MailRecipients MR ON MR.RecipientId = R.Id
LEFT JOIN data.MailFiles MF ON MR.MailFileId = MF.CorrelationId AND I.VerticalId = MF.VerticalId
WHERE A.Id <> 0
GROUP BY A.Id, I.VerticalId;
```

The `LEFT JOIN` chain to `MailRecipients`/`MailFiles` means recipients with zero mailings still appear (with `TimesMailed = 0` and `NULL` dates), which is necessary for the mail planning workflow to find unmailed leads. The grouping by `AddressId + VerticalId` rather than by `RecipientId` is a deliberate business decision: the same physical address should not receive the same vertical's mail twice, even if different recipient records exist for that address. The `MIN(R.Id)` picks a canonical recipient for downstream joins.

The `vw_multi_leads` view uses `DENSE_RANK()` windowing to identify leads that appear multiple times for the same recipient, ordered by import date:

```sql
-- From 20250509/04A_vw_multi_leads.sql
CREATE OR ALTER VIEW [data].[vw_multi_leads] AS
SELECT
    RecipientId, I.VerticalId,
    DENSE_RANK() OVER (
        PARTITION BY L.recipientId
        ORDER BY I.CreatedOn
    ) AS ImportOrder,
    I.PublisherId
FROM data.leads L
INNER JOIN data.Imports I ON I.CorrelationId = L.ImportId
WHERE L.ScrubbedReason = (1 << 20);
```

The filter `ScrubbedReason = (1 << 20)` restricts to leads whose only scrub reason is external duplicate (bit 20). This identifies re-purchased leads — recipients who appeared in a later import after already being in the system. The `DENSE_RANK` by `CreatedOn` establishes import ordering so the business can track which publisher supplied the lead first versus which supplied a duplicate.

---

## Key Files

- `madera-apps:Madera/Madera.DbMigrations/Migrations/DirectMail/20250407/06D_vw_leads_scrub.sql` — Bitflag decomposition with GET_BIT and priority UDF
- `madera-apps:Madera/Madera.DbMigrations/Migrations/DirectMail/20250509/04A_vw_recipient_mail_summary.sql` — Address+vertical mail frequency with COUNT(DISTINCT) and LEFT JOINs
- `madera-apps:Madera/Madera.DbMigrations/Migrations/DirectMail/20250509/04A_vw_multi_leads.sql` — DENSE_RANK duplicate lead detection
