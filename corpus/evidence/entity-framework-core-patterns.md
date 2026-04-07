---
title: Entity Framework Core Usage Patterns — Strategic ORM Selection
tags: [entity-framework-core, ef-core, csharp, postgresql, sql-server, masstransit, saga-persistence, dapper, orm-selection, linq, dbcontext, fluent-api]
children:
  - evidence/entity-framework-core-patterns-saga-persistence.md
  - evidence/entity-framework-core-patterns-reporting.md
  - evidence/entity-framework-core-patterns-corpus-cli.md
related:
  - evidence/dapper-async-data-access.md
  - evidence/dapper-async-data-access-pagination.md
  - evidence/domain-driven-modeling.md
  - evidence/dependency-injection-composition.md
  - projects/kb-platform.md
category: evidence
contact: resume@bryanboettcher.com
---

# Entity Framework Core Usage Patterns — Strategic ORM Selection — Index

Bryan's codebases use both Dapper and Entity Framework Core, often in the same project. The choice between them is not arbitrary — each ORM fills a specific role based on the data access pattern required. EF Core appears in three projects: kb-platform (catalog service with MassTransit saga persistence on PostgreSQL), madera-apps (reporting services on SQL Server), and the resume chatbot (corpus analysis pipeline on PostgreSQL). In every case, EF Core is chosen for a concrete reason: saga state machine persistence, LINQ-translated reporting aggregations, or schema management with typed queries.

The two ORMs are not in competition. They coexist behind shared interfaces (`IQuery<TModel, TParams>` in madera-apps) or in the same class (`CorpusDatabase` in resume-chat), with each handling the access pattern it was designed for.

The full evidence is split into focused documents:

## Child Documents

- **[MassTransit Saga Persistence (kb-platform)](entity-framework-core-patterns-saga-persistence.md)** — `SagaDbContext` with `ProductEntity` and `InventoryEntity`. `SagaClassMap<T>` with PostgreSQL `xid` column type for optimistic concurrency. Optimistic concurrency for domain sagas vs pessimistic for job service sagas. CQRS read side with anonymous-type projections: materialize to anonymous primitives first, then map to domain models in memory.

- **[Reporting Aggregations and Dual ORM (madera-apps)](entity-framework-core-patterns-reporting.md)** — `ReportingDbContext` mapping SQL Server views with `HasNoKey()`. Complex LINQ GROUP BY with 15+ computed columns (margins, break-even, cost-per-connect) with dynamically composable WHERE clauses. The dual ORM pattern: `SqlPaginatedServiceBase<T>` (Dapper) and `EfCorePaginatedServiceBase<T>` (EF Core) both implementing `IQuery<TModel, TParams>`.

- **[Schema Management and Typed Queries (resume-chat)](entity-framework-core-patterns-corpus-cli.md)** — `CorpusDbContext` fluent API with explicit column naming, named indexes, and alternate keys. EF Core for `EnsureCreatedAsync` and typed query composition, raw Npgsql for `ON CONFLICT` upserts with `xmax` to distinguish inserts from updates. Also covers the ORM selection decision framework.

