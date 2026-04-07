---
title: Entity Framework Core Patterns — Reporting Aggregations and Dual ORM (madera-apps)
tags: [entity-framework-core, ef-core, csharp, sql-server, dapper, orm-selection, linq, reporting, groupby, fluent-api]
related:
  - evidence/entity-framework-core-patterns.md
  - evidence/entity-framework-core-patterns-saga-persistence.md
  - evidence/entity-framework-core-patterns-corpus-cli.md
  - evidence/dapper-async-data-access.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/entity-framework-core-patterns.md
---

# Entity Framework Core Patterns — Reporting Aggregations and Dual ORM (madera-apps)

The madera-apps UI server uses EF Core for reporting queries against SQL Server views. This is the same codebase where the pipeline layer uses Dapper exclusively — the two ORMs coexist in the same project behind the shared `IQuery<TModel, TParams>` interface.

---

## Evidence: Reporting Aggregations with LINQ

The `ReportingDbContext` maps to SQL Server views (not tables) using `HasNoKey()`:

```csharp
// madera-apps:Madera/Madera.UI.Server/DbContext/ReportingDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<MailFilePerformanceReportModel>(entity =>
    {
        entity.ToTable("MailFilePerformance", "reports");
        entity.HasNoKey();
    });

    modelBuilder.Entity<MailPlanningReportModel>(entity =>
    {
        entity.ToTable("MailPlanning", "reports");
        entity.HasNoKey();
    });
}
```

The `MailFilePerformanceSummaryService` builds complex grouping and aggregation queries entirely in LINQ. This is the kind of query where EF Core's LINQ-to-SQL translation earns its weight — computing margins, break-even calculations, and per-connect revenue across grouped mail campaigns with optional filters:

```csharp
// madera-apps:Madera/Madera.UI.Server/Services/MailFilePerformanceReportService.cs
protected override IQueryable<MailFilePerformanceSummaryModel> BuildQuery(
    MailFilePerformanceSummaryQuery filters)
{
    var query = _context.MailFilePerformance.AsQueryable();

    if (filters.VerticalId.HasValue)
        query = query.Where(x => x.VerticalId == filters.VerticalId.Value);
    // ... additional optional filters for Broker, MailHouse, Publisher[], Creative[], date ranges

    var results = (
        from row in query
        group row by new
        {
            row.InboundPhoneNumber, row.MailDate, row.CreativeName,
            row.VerticalName, row.Filename, row.RecipientCount,
            row.MailCost, row.RecipientCost, row.CallsOver30Minutes,
            row.BreakEvenDate
        } into g
        select new MailFilePerformanceSummaryModel
        {
            ConnectedCount = g.Sum(x => x.ConnectedCalls),
            BillableCallCount = g.Sum(x => x.BillableCalls),
            TotalRevenue = g.Sum(x => x.Revenue),
            TotalCost = (g.Key.MailCost + g.Key.RecipientCost) * g.Key.RecipientCount,
            CostPerConnect = g.Sum(x => x.ConnectedCalls) > 0
                ? ((g.Key.MailCost + g.Key.RecipientCost) * g.Key.RecipientCount) / g.Sum(x => x.ConnectedCalls)
                : 0,
            Margin = g.Sum(x => x.Revenue) > 0
                ? (g.Sum(x => x.Revenue) - ((g.Key.MailCost + g.Key.RecipientCost) * g.Key.RecipientCount)) / g.Sum(x => x.Revenue)
                : 0,
            // ... 15+ computed columns
        } into o
        orderby o.MailDate descending
        select o
    );

    return results;
}
```

The LINQ approach lets each filter compose independently, and the grouping/aggregation translates cleanly to SQL GROUP BY. Meanwhile, the same project's import pipeline uses Dapper for stored procedure calls, table-valued parameters, and streaming queries where EF Core would add unnecessary overhead.

---

## Evidence: Dual ORM Behind Shared Interface

The `Querying.cs` file defines both `SqlPaginatedServiceBase<TModel, TParams>` (Dapper) and `EfCorePaginatedServiceBase<TModel, TParams>` (EF Core), both implementing the same `IQuery<TModel, TParams>` interface:

```csharp
// madera-apps:Madera/Madera.UI.Server/Services/Querying.cs
public interface IQuery<TModel, in TParams> where TModel : class where TParams : BaseQueryPayload
{
    Task<PaginatedResult<TModel>> Search(TParams parameters, CancellationToken token);
}

// Dapper implementation
public abstract class SqlPaginatedServiceBase<TModel, TParams> : IQuery<TModel, TParams>
{
    protected abstract string Query { get; }
    // Uses IConnectionProvider, Dapper QueryMultipleAsync/QueryFirstOrDefaultAsync
}

// EF Core implementation
public abstract class EfCorePaginatedServiceBase<TModel, TParams> : IQuery<TModel, TParams>
{
    protected readonly ReportingDbContext _context;
    protected abstract IQueryable<TModel> BuildQuery(TParams parameters);
    // Uses DbContext, LINQ, ToListAsync/FirstOrDefaultAsync
}
```

Concrete services pick the appropriate base class. The `SqlImportLogService` extends `SqlPaginatedServiceBase` because it runs a stored procedure (`ReportSql.PaginatedGetImports`). The `MailFilePerformanceSummaryService` and `MailPlanningSummaryService` extend `EfCorePaginatedServiceBase` because their queries involve composable filtering and GROUP BY aggregations that benefit from LINQ translation. The consuming code (API endpoints) only sees `IQuery<TModel, TParams>` and is unaware of which ORM fulfills the request.

---

## Key Files

- `madera-apps:Madera/Madera.UI.Server/Services/Querying.cs` — Dual Dapper/EF Core base classes behind shared IQuery interface
- `madera-apps:Madera/Madera.UI.Server/DbContext/ReportingDbContext.cs` — SQL Server view mapping with HasNoKey
- `madera-apps:Madera/Madera.UI.Server/Services/MailFilePerformanceReportService.cs` — Complex LINQ GROUP BY with 15+ computed columns
- `madera-apps:Madera/Madera.UI.Server/Services/MailPlanningReportService.cs` — Composable LINQ reporting with optional filters
- `madera-apps:Madera/Madera.UI.Server/Registries/ReportingRegistry.cs` — DbContext registration with IConnectionStringProvider options
