---
title: Domain-Driven Modeling — Entities as Saga State Machine Instances (kb-platform)
tags: [ddd, entity-design, state-machines, masstransit, csharp, dotnet, ef-core, postgresql, saga-orchestration, optimistic-concurrency]
related:
  - evidence/domain-driven-modeling.md
  - evidence/domain-driven-modeling-value-types.md
  - evidence/domain-driven-modeling-compound-value-objects.md
  - evidence/distributed-systems-architecture.md
  - evidence/entity-framework-core-patterns.md
  - projects/kb-platform-architecture.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/domain-driven-modeling.md
---

# Domain-Driven Modeling — Entities as Saga State Machine Instances (kb-platform)

Kb-platform uses MassTransit's `SagaStateMachineInstance` as its aggregate root pattern. Domain entities are not passive data containers — they are the persistence model for state machines that enforce lifecycle transitions. Three bounded contexts follow this pattern.

---

## Evidence: Three Saga Entities

**ProductEntity** (Catalog domain):

```csharp
// kb-platform:src/services/KbStore.Catalog/Domains/Products/ProductEntity.cs
public class ProductEntity : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public uint RowVersion { get; set; }
    public int CurrentState { get; set; }

    public string Sku { get; set; } = "";
    public string? Name { get; set; }
    public int Quantity { get; set; }
    // ... dimensions, stock fields ...

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

**InventoryEntity** (Catalog domain):

```csharp
// kb-platform:src/services/KbStore.Catalog/Domains/Inventory/InventoryEntity.cs
public sealed class InventoryEntity : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public uint RowVersion { get; set; }
    public int CurrentState { get; set; }

    public string PartNumber { get; set; } = "";
    public int StockQuantity { get; set; }

    public InventoryStatus Status => CurrentState switch
    {
        InventoryStates.Available => InventoryStatus.Available,
        InventoryStates.OnHold => InventoryStatus.Held,
        InventoryStates.Backordered => InventoryStatus.Backordered,
        InventoryStates.Discontinued => InventoryStatus.Discontinued,
        _ => InventoryStatus.Invalid
    };
}
```

**SellableItemEntity** (Storefront domain):

```csharp
// kb-platform:src/services/KbStore.Storefront/Domains/SellableItems/SellableItemEntity.cs
public class SellableItemEntity : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public int Version { get; set; }
    public int CurrentState { get; set; }

    public PricingInfoData Pricing { get; set; } = new();
    public SellableItemPayload? Payload { get; set; }
    public bool IsAvailable { get; set; }
}
```

---

## Key Modeling Decisions

1. **Computed behavioral properties derived from state.** `IsEnabled`, `IsAvailable`, and `Status` are not stored columns — they are switch expressions over `CurrentState`. The entity exposes domain-meaningful booleans and enums while the state machine integer remains an internal concern. Callers never inspect `CurrentState` directly.

2. **Optimistic concurrency via `RowVersion` (EF Core/PostgreSQL `xid`) or `ISagaVersion`.** The Catalog entities use PostgreSQL's row versioning. The Storefront entity uses MassTransit's `ISagaVersion` interface, reflecting that it uses a MongoDB-backed saga repository rather than EF Core.

3. **Entity-map co-location.** `ProductSagaMap` and `InventorySagaMap` are defined in the same file as their entities, configuring EF Core column types, max lengths, unique indexes, and foreign keys. This keeps the persistence mapping adjacent to the domain model rather than in a separate mapping assembly.

4. **Cross-entity relationships.** `ProductEntity` has a navigation property to `InventoryEntity` via `InventoryId`, but the relationship is managed by the state machine — inventory lifecycle events (`InventoryQuantityChanged`, `InventoryDiscontinued`, `InventoryDeleted`) flow back to update product state.

5. **Entity-as-saga-instance.** Making domain entities implement `SagaStateMachineInstance` directly (rather than having separate saga and domain entity classes) means the persistence model, the domain model, and the state machine state are a single object. This eliminates mapping layers between saga state and domain state, at the cost of coupling the entity shape to MassTransit's persistence requirements.

---

## Key Files

- `kb-platform:src/services/KbStore.Catalog/Domains/Products/ProductEntity.cs` — Saga entity with computed IsEnabled/IsAvailable from state
- `kb-platform:src/services/KbStore.Catalog/Domains/Inventory/InventoryEntity.cs` — Saga entity with Status enum derived from state machine position
- `kb-platform:src/services/KbStore.Storefront/Domains/SellableItems/SellableItemEntity.cs` — Saga entity with embedded PricingInfoData value object (ADR-003)
