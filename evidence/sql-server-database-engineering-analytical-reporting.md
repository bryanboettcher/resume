---
title: SQL Server Database Engineering — Analytical Reporting with Chained CTEs and UTF-8 Encoding
tags: [sql-server, cte, window-functions, reporting, t-sql, generate-series, utf8, analytical-queries]
related:
  - evidence/sql-server-database-engineering.md
  - evidence/sql-server-database-engineering-snapshot-isolation.md
  - evidence/sql-server-database-engineering-staging-pipeline.md
  - evidence/data-engineering-etl.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/sql-server-database-engineering.md
---

# SQL Server Database Engineering — Analytical Reporting with Chained CTEs and UTF-8 Encoding

The Madera platform's `usp_UpdateMailFilePerformance` procedure chains three CTEs to compute per-campaign break-even dates: daily revenue aggregation, cumulative running totals via `ROWS UNBOUNDED PRECEDING`, and the first date cumulative revenue exceeds cost. The codebase also includes a pure T-SQL UTF-8 encoder using `GENERATE_SERIES` (a SQL Server 2022 addition), implementing RFC 3629 for interop with external systems.

---

## Evidence: Analytical Reporting with Chained CTEs

The `usp_UpdateMailFilePerformance` procedure (140 lines) chains three CTEs to compute daily revenue, cumulative revenue with running window functions, and break-even date calculation:

```sql
-- From 20250523/05A_usp_updateMailFilePerformance.sql
WITH DailyCallSummary AS (
    SELECT mf.CorrelationId, ...,
        COUNT(*) AS ConnectedCalls,
        SUM(CASE WHEN rcl.IsConverted = 1 THEN 1 ELSE 0 END) AS BillableCalls,
        SUM(ISNULL(rcl.ConversionAmount, 0)) AS Revenue,
        SUM(CASE WHEN ISNULL(rcl.CallLength, 0) > 1800 THEN 1 ELSE 0 END) AS CallsOver30Minutes
    FROM [data].[MailFiles] mf
    INNER JOIN [data].[RingbaCallLogs] rcl ON mf.InboundPhoneNumber = rcl.InboundPhoneNumber
    GROUP BY ...
),
CumulativeRevenue AS (
    SELECT *,
        SUM(Revenue) OVER (PARTITION BY CorrelationId ORDER BY CallDate
            ROWS UNBOUNDED PRECEDING) AS CumulativeRevenue,
        (MailCost + RecipientCost) AS TotalCost
    FROM DailyCallSummary
),
BreakEvenCalculation AS (
    SELECT CorrelationId,
        MIN(CASE WHEN CumulativeRevenue >= TotalCost THEN CallDate ELSE NULL END)
            AS BreakEvenDate
    FROM CumulativeRevenue GROUP BY CorrelationId
)
```

This computes the first date each mail campaign's cumulative call revenue exceeds its cost (mail printing + data acquisition), giving the business a per-campaign break-even timeline. The `ROWS UNBOUNDED PRECEDING` window frame makes the running total correct for partitioned time-series aggregation.

---

## Evidence: Pure T-SQL UTF-8 Encoding Function

The `ufn_StringToUTF8Bytes` function (85 lines, flagged as high complexity) implements RFC 3629 UTF-8 encoding entirely in T-SQL using `GENERATE_SERIES` and set-based UNION ALL for multi-byte character expansion:

```sql
-- From 20250128/01F_ufn_stringToUtf8Bytes.sql
WITH CharacterBytes AS (
    SELECT n.[Value],
        CodePoint = UNICODE(SUBSTRING(@input, [Value], 1)),
        ByteCount = CASE
            WHEN UNICODE(SUBSTRING(@input, [Value], 1)) <= 0x7F THEN 1
            WHEN UNICODE(SUBSTRING(@input, [Value], 1)) <= 0x7FF THEN 2
            WHEN UNICODE(SUBSTRING(@input, [Value], 1)) <= 0xFFFF THEN 3
            ELSE 4
        END
    FROM GENERATE_SERIES(1, @max) n
),
UTF8Bytes AS (
    -- Lead byte: 0xC0 + (CodePoint / 0x40) for 2-byte, etc.
    SELECT c.[Value], ByteNum = 1, ByteValue = CAST(CASE c.ByteCount
        WHEN 1 THEN c.CodePoint
        WHEN 2 THEN 0xC0 + (c.CodePoint / 0x40)
        WHEN 3 THEN 0xE0 + (c.CodePoint / 0x1000)
        ELSE 0xF0 + (c.CodePoint / 0x40000)
    END AS TINYINT) FROM CharacterBytes c
    UNION ALL
    -- Continuation bytes: 0x80 + (CodePoint % 0x40)
    ...
)
```

This is a table-valued function that returns individual bytes with position — useful for hashing or binary protocol construction where SQL Server's native `NVARCHAR` encoding needs to be converted to UTF-8 byte sequences for interop with external systems. `GENERATE_SERIES` is a SQL Server 2022 addition that replaces the older tally table pattern.

## Key Files

- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250523/05A_usp_updateMailFilePerformance.sql` — Chained CTEs with window functions for break-even analysis
- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20250128/01F_ufn_stringToUtf8Bytes.sql` — Pure T-SQL UTF-8 encoder
