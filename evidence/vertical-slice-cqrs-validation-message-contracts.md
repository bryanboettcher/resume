---
title: Vertical Slice CQRS — Interface-Based Message Contracts
tags: [cqrs, masstransit, message-driven, interface-design, contract-design, csharp, deterministic-guid, idempotency]
related:
  - evidence/vertical-slice-cqrs-validation.md
  - evidence/vertical-slice-cqrs-validation-structure.md
  - evidence/vertical-slice-cqrs-validation-exception-hierarchy.md
  - evidence/vertical-slice-cqrs-validation-cross-domain-propagation.md
  - evidence/masstransit-contract-design.md
  - evidence/distributed-systems-architecture.md
  - projects/kbstore-ecommerce.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/vertical-slice-cqrs-validation.md
---

# Vertical Slice CQRS — Interface-Based Message Contracts

In the KbStore e-commerce platform (kb-platform), all MassTransit message contracts are defined as interfaces with a consistent per-domain hierarchy. A `CorrelatedProductCommand` interface enables middleware-based deterministic GUID computation from SKUs before the state machine's correlation logic runs, eliminating EF Core client-side evaluation issues and enabling idempotent command handling across all three bounded contexts.

---

## Evidence: Interface-Based Message Contracts

All MassTransit message contracts are defined as interfaces, not classes. This is a deliberate MassTransit pattern: the framework's anonymous-type serialization means commands are constructed as `new { PropertyName = value }` and the bus serializes them against the interface contract. No concrete DTO classes exist for messages.

The contracts follow a consistent hierarchy per domain:

```csharp
// src/services/KbStore.Catalog.Abstractions/Contracts/Products.cs

// Base command — all commands carry ProductId and Timestamp
public interface ProductCommand : CorrelatedBy<Guid>
{
    Guid ProductId { get; }
    DateTimeOffset Timestamp { get; }
}

// Request/response pairs for each operation
public interface UpdateProductNameRequest : ProductCommand, CorrelatedProductCommand
{
    string? Name { get; }
}
public interface UpdateProductResponse : ProductModel;

// Event hierarchy for downstream consumers
public interface ProductUpdated : BaseProductEvent;
public interface ProductNameUpdated : ProductUpdated;
```

Every command interface also extends `CorrelatedProductCommand`, which enables middleware-based correlation. The `CorrelatedProductCommand` interface requires a settable `CorrelationId` and a `Sku` property, allowing middleware to compute deterministic GUIDs from SKUs before the state machine's correlation logic executes. This eliminates database-level client-side evaluation issues with EF Core and enables idempotent command handling.

**Source:** `src/services/KbStore.Catalog.Abstractions/Contracts/CorrelatedProductCommand.cs`

The same pattern is replicated in the Storefront domain via `SellableItemCommand`, which uses `DeterministicGuid.FromSellableItemSku(Sku)` for MongoDB-backed saga instances.

## Key Files

- `kb-platform:src/services/KbStore.Catalog.Abstractions/Contracts/Products.cs` — Interface-based message contracts
- `kb-platform:src/services/KbStore.Catalog.Abstractions/Contracts/CorrelatedProductCommand.cs` — Middleware correlation contract
- `kb-platform:src/services/KbStore.Storefront.Abstractions/Contracts/SellableItemCommands.cs` — Storefront command contracts
