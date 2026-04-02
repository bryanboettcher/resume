---
title: Entity Framework Core Patterns — MassTransit Saga Persistence (kb-platform)
tags: [entity-framework-core, ef-core, csharp, postgresql, masstransit, saga-persistence, orm-selection, fluent-api, optimistic-concurrency, cqrs]
related:
  - evidence/entity-framework-core-patterns.md
  - evidence/entity-framework-core-patterns-reporting.md
  - evidence/entity-framework-core-patterns-corpus-cli.md
  - evidence/domain-driven-modeling.md
  - evidence/dapper-async-data-access.md
  - projects/kb-platform-architecture.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/entity-framework-core-patterns.md
---

# Entity Framework Core Patterns — MassTransit Saga Persistence (kb-platform)

The kb-platform Catalog service uses EF Core as its persistence layer for MassTransit state machines. The `ApplicationDbContext` inherits from `SagaDbContext` (from `MassTransit.EntityFrameworkCoreIntegration`), which provides the infrastructure for MassTransit to load and save saga instances as EF Core entities.

---

## Evidence: SagaDbContext and Entity Configuration

```csharp
// kb-platform:src/services/KbStore.Catalog/Persistence/ApplicationDbContext.cs
public class ApplicationDbContext : SagaDbContext
{
    public DbSet<InventoryEntity> Inventory { get; set; }
    public DbSet<ProductEntity> Products { get; set; }

    protected override IEnumerable<ISagaClassMap> Configurations =>
    [
        new InventorySagaMap(),
        new ProductSagaMap()
    ];
}
```

The saga entity classes implement `SagaStateMachineInstance` and combine domain data with state machine tracking. `ProductEntity` stores both business properties (SKU, dimensions, pricing) and saga infrastructure (`CorrelationId`, `CurrentState` as an integer, `RowVersion` for optimistic concurrency). Computed behavioral properties derive from the state machine position:

```csharp
// kb-platform:src/services/KbStore.Catalog/Domains/Products/ProductEntity.cs
public class ProductEntity : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public uint RowVersion { get; set; }
    public int CurrentState { get; set; }

    public bool IsEnabled => CurrentState switch
    {
        ProductStates.Enabled => true,
        ProductStates.Disabled => false,
        ProductStates.Discontinued => false,
        _ => false
    };
    public bool IsAvailable => IsStocked && IsEnabled;
}
```

The `SagaClassMap<T>` implementations configure PostgreSQL-specific column types and constraints. The `RowVersion` property maps to PostgreSQL's `xid` system column type for optimistic concurrency:

```csharp
// kb-platform:src/services/KbStore.Catalog/Domains/Products/ProductEntity.cs
public class ProductSagaMap : SagaClassMap<ProductEntity>
{
    protected override void Configure(EntityTypeBuilder<ProductEntity> entity, ModelBuilder model)
    {
        entity.Property(x => x.RowVersion)
            .HasColumnType("xid")
            .IsRowVersion();

        entity.Property(x => x.Sku)
            .HasMaxLength(32)
            .IsRequired();

        entity.HasIndex(x => x.Sku).IsUnique();

        entity.HasOne(p => p.Inventory)
            .WithMany()
            .HasForeignKey("InventoryId")
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
```

---

## Evidence: Saga Registration — Optimistic vs Pessimistic Concurrency

The saga registration in `HostBuilderExtensions.cs` wires the state machines to EF Core with optimistic concurrency for domain sagas and pessimistic concurrency for the MassTransit job service:

```csharp
// kb-platform:src/services/KbStore.Catalog/Extensions/HostBuilderExtensions.cs
bus.AddSagaStateMachine<InventoryStateMachine, InventoryEntity>()
    .EntityFrameworkRepository(repo =>
    {
        repo.ConcurrencyMode = ConcurrencyMode.Optimistic;
        repo.ExistingDbContext<ApplicationDbContext>();
        repo.UsePostgres();
    });

// Job service sagas use pessimistic concurrency
bus.AddJobSagaStateMachines()
    .EntityFrameworkRepository(repo =>
    {
        repo.ConcurrencyMode = ConcurrencyMode.Pessimistic;
        repo.ExistingDbContext<JobServiceSagaDbContext>();
        repo.UsePostgres();
    });
```

This is the opposite strategy from the madera-apps codebase, where saga persistence uses custom Dapper-based `DatabaseContext<T>` implementations with explicit `DbConnection` + `DbTransaction` pairs and `RepeatableRead` isolation. The difference is driven by the database: kb-platform targets PostgreSQL (where EF Core's `xid`-based optimistic concurrency is a natural fit), while madera-apps targets SQL Server with stored-procedure-heavy patterns that Dapper handles directly.

---

## Evidence: CQRS Read Side with Projections

The Catalog service separates commands (routed through MassTransit request/response to the state machine) from queries (executed directly against the DbContext). The query services use EF Core LINQ with explicit anonymous-type projections to control which columns hit the wire, then materialize before mapping to domain models:

```csharp
// kb-platform:src/services/KbStore.Catalog.Services/DbContextProductQueryService.cs
var serverQuery = baseQuery
    .Skip(page * size)
    .Take(size)
    .Select(entity => new  // Anonymous primitives for EF
    {
        ProductId = entity.CorrelationId,
        entity.Sku,
        entity.Name,
        // ... other fields ...
        entity.CurrentState,
        entity.CreatedOn, entity.UpdatedOn
    });

// Materialize from database before mapping to models
var materializedData = await serverQuery.ToListAsync(cancellationToken)
    .ConfigureAwait(false);

var results = materializedData
    .Select(x => new ProductReadModel(/* ... */))
    .ToList();
```

The comment `// Anonymous primitives for EF` captures why the projection exists: EF Core's LINQ translator works reliably with anonymous types containing primitive properties, but can fail on complex computed properties or constructor calls. Materializing to an anonymous type first, then mapping to the domain model in memory, avoids translation failures while keeping the SQL projection narrow.

The DI registration shows the CQRS split:

```csharp
// kb-platform:src/services/KbStore.Catalog.Services/Extensions/ServiceCollectionExtensions.cs
services.AddNpgsql<ApplicationDbContext>(config.GetConnectionString("catalog"));

services.AddScoped<IInventoryCommandService, MassTransitInventoryCommandService>();
services.AddScoped<IInventoryQueryService, DbContextInventoryQueryService>();
```

---

## Key Files

- `kb-platform:src/services/KbStore.Catalog/Persistence/ApplicationDbContext.cs` — SagaDbContext with ProductEntity and InventoryEntity DbSets
- `kb-platform:src/services/KbStore.Catalog/Domains/Products/ProductEntity.cs` — Saga entity with fluent API configuration, xid row versioning, navigation properties
- `kb-platform:src/services/KbStore.Catalog/Domains/Inventory/InventoryEntity.cs` — Saga entity with computed status from state machine position
- `kb-platform:src/services/KbStore.Catalog/Extensions/HostBuilderExtensions.cs` — MassTransit saga registration with EF Core repository, optimistic vs pessimistic concurrency
- `kb-platform:src/services/KbStore.Catalog.Services/DbContextProductQueryService.cs` — CQRS read side with anonymous-type projections and private read models
- `kb-platform:src/services/KbStore.Catalog.Services/Extensions/ServiceCollectionExtensions.cs` — CQRS DI split: MassTransit commands, DbContext queries
