---
title: KB3D E-Commerce Platform — Architecture Deep Dive
tags: [dotnet, masstransit, angular, ddd, event-driven, postgresql, mongodb, aspire, rabbitmq, cqrs, saga-orchestration, state-machines, playwright, polyglot-persistence, deterministic-guid, middleware, hangfire]
children:
  - projects/kb-platform-architecture-domain-design.md
  - projects/kb-platform-architecture-infrastructure.md
related:
  - projects/kbstore-ecommerce.md
  - evidence/vertical-slice-cqrs-validation.md
  - evidence/testing-infrastructure.md
  - evidence/dependency-injection-composition.md
  - evidence/domain-driven-modeling.md
  - evidence/masstransit-consumer-patterns.md
  - evidence/masstransit-contract-design.md
  - evidence/aspnet-minimal-api-patterns.md
category: project
contact: resume@bryanboettcher.com
---

# KB3D E-Commerce Platform — Architecture Deep Dive — Index

KB3D (KbStore) is a greenfield distributed e-commerce platform for an RC hobby shop, designed from the ground up with Domain-Driven Design, event sourcing, and CQRS. The codebase spans 539 source files and approximately 97,000 lines of code across three bounded contexts, a shared infrastructure layer, an Angular admin UI, and Playwright end-to-end tests.

## Technology Stack

**Backend**: .NET (C#), ASP.NET Core Minimal APIs, MassTransit (saga state machines, request/response, outbox, job consumers, Hangfire scheduling), Entity Framework Core (PostgreSQL saga persistence), Hangfire (background jobs with PostgreSQL storage), RabbitMQ

**Frontend**: Angular (standalone components), TypeScript, PrimeNG, Playwright (E2E testing), Jest + Mock Service Worker (unit/integration testing)

**Infrastructure**: .NET Aspire (service orchestration, service discovery, OpenTelemetry), Docker, PostgreSQL, MongoDB

**Testing**: NUnit (BDD nested classes), NSubstitute, Shouldly, MassTransit Test Harness, Aspire Testing Builder, Playwright

## Child Documents

- **[Bounded Contexts, State Machines, and Deterministic GUIDs](kb-platform-architecture-domain-design.md)** — Polyglot persistence selection (Catalog/PostgreSQL, Storefront/MongoDB, ApiService/no state). Three saga state machines as domain aggregates: Inventory (idempotent create, resurrection, hold semantics, two-phase delete), Product (cross-aggregate inventory validation, cascade from inventory events), SellableItem (polymorphic payloads, tag management). Zero-allocation deterministic GUID generation with `stackalloc`. Correlation middleware converting business-key queries to constant GUID lookups. Fault-to-typed-exception reconstitution pattern.

- **[Aspire Orchestration, Service Layer, and Testing](kb-platform-architecture-infrastructure.md)** — Single AppHost composition with `WaitFor()` dependency ordering, Angular as `JavaScriptApp`. Service layer as strategic boundary: CQRS split (MassTransit commands, EF Core queries), injected time provider, pre-validation. Decomposed MassTransit bus configuration (features, job consumers, sagas, transport). In-memory outbox for atomic event publication. Three-layer testing: state machine unit tests with 250ms timeout harness, command service tests with controlled timestamps, full Aspire integration tests with real containers, Playwright E2E tests.

