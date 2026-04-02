---
title: Vertical Slice CQRS with MassTransit Request/Response
tags: [cqrs, masstransit, vertical-slice, validation, request-response, domain-exceptions, ddd, aspnet-core, testing, nunit, event-driven]
related:
  - evidence/distributed-systems-architecture.md
  - evidence/dotnet-csharp-expertise.md
  - projects/kbstore-ecommerce.md
children:
  - evidence/vertical-slice-cqrs-validation-structure.md
  - evidence/vertical-slice-cqrs-validation-layered-validation.md
  - evidence/vertical-slice-cqrs-validation-exception-hierarchy.md
  - evidence/vertical-slice-cqrs-validation-message-contracts.md
  - evidence/vertical-slice-cqrs-validation-cross-domain-propagation.md
  - evidence/vertical-slice-cqrs-validation-test-harness.md
category: evidence
contact: resume@bryanboettcher.com
---

# Vertical Slice CQRS with MassTransit Request/Response — Index

The KbStore e-commerce platform (kb-platform) implements a consistent vertical slice architecture where every write operation follows the same structural pattern from HTTP endpoint through message bus to state machine. This is not a simple REST-over-database CRUD layer — each command traverses a multi-layer validation pipeline and crosses a message boundary before reaching the domain. Three bounded contexts (Catalog/Product, Catalog/Inventory, Storefront/SellableItem) implement approximately 30 distinct vertical slices sharing a uniform architectural shape.

**Repository:** https://github.com/bryanboettcher/KbStore

## Child Documents

- **[The Vertical Slice Structure](vertical-slice-cqrs-validation-structure.md)** — The five-layer pattern (HTTP endpoint → command service → MassTransit R/R → state machine → cross-domain consumer) applied consistently across ~30 operations in three bounded contexts.

- **[Layered Validation Without a Framework](vertical-slice-cqrs-validation-layered-validation.md)** — Three-layer validation without FluentValidation: endpoint `IsValid()` for structural checks, command service exceptions for domain rules, state machine for transition guards. The bus only sees commands that cleared both prior layers.

- **[Typed Exception Hierarchy with Fault Reconstitution](vertical-slice-cqrs-validation-exception-hierarchy.md)** — Per-bounded-context exception hierarchies (`ProductException`, `ProductNotFoundException`, `ProductConflictException`, etc.) and the `ToProductException()` extension that reconstitutes typed exceptions from `RequestFaultException` — making the message boundary transparent to callers.

- **[Interface-Based Message Contracts](vertical-slice-cqrs-validation-message-contracts.md)** — All contracts as interfaces, the `CorrelatedProductCommand` pattern for middleware-computed deterministic GUIDs from SKUs, and how the same structure is replicated in the Storefront domain with MongoDB-backed sagas.

- **[Cross-Domain Event Propagation](vertical-slice-cqrs-validation-cross-domain-propagation.md)** — API gateway consumers that bridge bounded contexts: `ProductCreated` → `CreateSellableItem`, explicit idempotency handling, and the full set of 10 event consumers for Catalog and Inventory lifecycle events.

- **[Test Harness for Vertical Slices](vertical-slice-cqrs-validation-test-harness.md)** — `EventingTestBase` with 250ms MassTransit request timeout, BDD `When_X` nested-class pattern, and the three-way test assertion (exception type, result population, `Harness.Consumed.Any<T>()`) that distinguishes validation failures from domain failures.
