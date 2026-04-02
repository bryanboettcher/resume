---
title: Vertical Slice CQRS — The Vertical Slice Structure
tags: [cqrs, masstransit, vertical-slice, aspnet-core, request-response, ddd, event-driven, domain-modeling]
related:
  - evidence/vertical-slice-cqrs-validation.md
  - evidence/vertical-slice-cqrs-validation-layered-validation.md
  - evidence/vertical-slice-cqrs-validation-exception-hierarchy.md
  - evidence/vertical-slice-cqrs-validation-message-contracts.md
  - evidence/vertical-slice-cqrs-validation-cross-domain-propagation.md
  - evidence/vertical-slice-cqrs-validation-test-harness.md
  - evidence/distributed-systems-architecture.md
  - projects/kbstore-ecommerce.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/vertical-slice-cqrs-validation.md
---

# Vertical Slice CQRS — The Vertical Slice Structure

The KbStore e-commerce platform (kb-platform) implements a consistent vertical slice architecture where every write operation follows the same structural pattern from HTTP endpoint through message bus to state machine. This is not a simple REST-over-database CRUD layer — each command traverses a multi-layer validation pipeline and crosses a message boundary before reaching the domain.

---

## Evidence: The Five-Layer Vertical Slice

Every write operation in KbStore follows a five-layer pattern:

1. **HTTP Endpoint** — payload validation, deserialization, route mapping
2. **Command Service** — domain validation, message construction, request/response dispatch
3. **MassTransit Request/Response** — typed command sent via `IClientFactory.CreateRequestClient<T>()`
4. **State Machine** — saga handles command, applies state transitions, publishes domain events
5. **Cross-Domain Consumer** — reacts to domain events, propagates changes across bounded contexts

Three bounded contexts (Catalog/Product, Catalog/Inventory, Storefront/SellableItem) each implement this pattern independently, with roughly 10 command operations per domain. The result is approximately 30 distinct vertical slices sharing a uniform architectural shape.

**Repository:** https://github.com/bryanboettcher/KbStore

**Key paths:**
- `src/services/KbStore.ApiService/Endpoints/` — HTTP layer
- `src/services/KbStore.Catalog.Services/` — Catalog command services
- `src/services/KbStore.Storefront.Services/` — Storefront command services
- `src/services/KbStore.Catalog.Abstractions/Contracts/` — message contracts
- `src/services/KbStore.ApiService/Consumers/` — cross-domain event consumers
