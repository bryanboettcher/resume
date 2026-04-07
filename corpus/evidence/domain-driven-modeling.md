---
title: Domain-Driven Modeling Patterns
tags: [ddd, value-objects, domain-modeling, entity-design, state-machines, masstransit, csharp, dotnet, sealed-classes, implicit-operators, iequatable]
related:
  - evidence/vertical-slice-cqrs-validation.md
  - evidence/vertical-slice-cqrs-validation-exception-hierarchy.md
  - evidence/masstransit-contract-design.md
  - evidence/distributed-systems-architecture.md
  - projects/kbstore-ecommerce.md
children:
  - evidence/domain-driven-modeling-value-types.md
  - evidence/domain-driven-modeling-entity-saga.md
  - evidence/domain-driven-modeling-compound-value-objects.md
category: evidence
contact: resume@bryanboettcher.com
---

# Domain-Driven Modeling Patterns — Index

Two production codebases — madera-apps (direct mail processing platform) and kb-platform (e-commerce) — apply domain-driven design modeling patterns at different scales. Madera-apps has a dedicated `ValueTypes/` namespace containing strongly-typed domain identifiers that replace primitive types throughout message contracts and data access layers. Kb-platform uses MassTransit saga entities as the aggregate root pattern, where domain entities implement `SagaStateMachineInstance` and expose computed behavioral properties derived from their state machine lifecycle. FastAddress contributes a `readonly struct` value object for address encoding in a high-performance search context.

Consistent philosophy across all three:

- **Primitive obsession avoidance**: A method signature like `GetByBroker(BrokerId brokerId)` is self-documenting in a way that `GetByBroker(int brokerId)` is not, and the type system prevents swapping a `BrokerId` for a `PublisherId` at compile time.
- **Entities with behavioral state**: Computed properties (`IsEnabled`, `IsAvailable`, `Status`) derive from state machine position, meaning the entity itself encodes business rules about what state combinations mean.
- **Validation at the domain boundary**: `GroupingFilter.Validate()`, `MailGrouping.ValidateAll()`, and exception factory methods all place validation logic inside domain objects rather than in a separate validation layer.

The full evidence is split into focused documents:

## Child Documents

- **[Strongly-Typed Domain Identifiers](domain-driven-modeling-value-types.md)** — The seven sealed value object classes in `Madera.Contracts.ValueTypes.DirectMail` (`BrokerId`, `PublisherId`, `MailFileId`, `AddressId`, `AddressHash`, `VerticalId`, `CorrelationId`). Private constructors, `IEquatable<T>`, bidirectional implicit conversion operators, nullable conversions for database interop. Why reference-type value objects rather than structs.

- **[Entities as Saga State Machine Instances](domain-driven-modeling-entity-saga.md)** — Kb-platform's three bounded context entities (`ProductEntity`, `InventoryEntity`, `SellableItemEntity`) implementing `SagaStateMachineInstance`. Computed behavioral properties from state machine position. Optimistic concurrency via PostgreSQL `xid` vs. MassTransit `ISagaVersion`. Entity-map co-location. Cross-entity relationships managed through state machine events.

- **[Compound Value Objects](domain-driven-modeling-compound-value-objects.md)** — `GpsCoordinates` readonly struct with SRID constant and nullable factory. `MailGrouping` self-validating aggregate with polymorphic `GroupingFilter` hierarchy (11 subtypes, `[JsonDerivedType]` discriminators, each with its own `Validate()` override). `PricingInfoData` ADR-003 refactoring. `AddressEncoding` readonly struct for high-performance search.
