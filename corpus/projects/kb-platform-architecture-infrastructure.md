---
title: KB3D E-Commerce Platform — Aspire Orchestration, Service Layer, and Testing
tags: [dotnet, aspire, masstransit, angular, playwright, testing, masstransit-test-harness, service-layer, cqrs, rabbitmq]
related:
  - projects/kb-platform-architecture.md
  - projects/kb-platform-architecture-domain-design.md
  - projects/kbstore-ecommerce.md
  - evidence/testing-infrastructure.md
  - evidence/dependency-injection-composition.md
category: project
contact: resume@bryanboettcher.com
parent: projects/kb-platform-architecture.md
---

# KB3D E-Commerce Platform — Aspire Orchestration, Service Layer, and Testing

This document covers the infrastructure composition, service layer design principles, MassTransit bus configuration, and the three-layer testing architecture.

---

## .NET Aspire Orchestration

The entire platform is orchestrated through a single Aspire AppHost. The composition declares infrastructure dependencies and wires service discovery:

```
RabbitMQ (persistent volume, management plugin)
PostgreSQL (persistent volume, PgWeb admin UI) → "catalog" database
MongoDB (persistent volume, MongoExpress admin UI) → "storefront" database
```

Services declare explicit dependency ordering via `WaitFor()`:

- `domain-catalog` waits for RabbitMQ and PostgreSQL
- `domain-storefront` waits for RabbitMQ and MongoDB
- `api` waits for RabbitMQ, both databases, and both domain services
- `admin-ui` (Angular) waits for the API service

The AppHost also registers the Angular AdminUI as a `JavaScriptApp` with `PublishAsDockerFile()`, enabling the frontend to participate in Aspire's service discovery and telemetry without a custom Docker setup.

---

## Service Layer as Strategic Boundary

Each bounded context exposes its functionality through a service layer that wraps MassTransit interactions behind domain service interfaces. This provides:

- **CQRS separation**: Command services use MassTransit request/response through `IClientFactory`. Query services bypass the message bus entirely, using EF Core `ApplicationDbContext` with projection queries and `IAsyncEnumerable` pagination.
- **Injected time provider**: `Func<DateTimeOffset>` is injected into command services so timestamps are controlled in tests (fixed `Now` and `Later` constants) while using `DateTimeOffset.UtcNow` in production.
- **Pre-validation**: Business rule checks (null SKU, negative quantity) run synchronously before dispatching async messages, providing fail-fast behavior without consuming messaging resources.

The service layer is the public API of each bounded context. The ApiService layer depends only on service interfaces from the Abstractions projects, not on state machine implementations or persistence details.

---

## MassTransit Infrastructure Configuration

The Catalog service's `HostBuilderExtensions` demonstrates the decomposed MassTransit configuration pattern:

- **ConfigureFeatures**: Discovers consumers, futures, and activities by assembly scan
- **ConfigureJobConsumers**: Registers Hangfire-backed job saga state machines with pessimistic concurrency — jobs require exactly-once execution guarantees
- **ConfigureSagas**: Registers `InventoryStateMachine` and `ProductStateMachine` with EF Core repositories and optimistic concurrency
- **ConfigureTransport**: Configures RabbitMQ host, correlation middleware pipeline, in-memory outbox, and scheduled message publishing

The in-memory outbox ensures that domain events are published atomically with saga state transitions. If the database transaction rolls back, the buffered events are discarded.

---

## Testing Architecture

The testing infrastructure spans three layers, each with a distinct purpose:

**State machine tests** use `EventingTestBase`, which spins up an in-memory MassTransit test harness with 250ms request timeouts (vs. MassTransit's 30-second default). A custom `TimeoutRequestClient<TRequest>` wrapper ensures tests fail fast rather than hanging. The base class follows Arrange/Act pattern with exception capture — `LastException` is set if `Act()` throws, allowing assertions on both success and failure paths without try/catch blocks in individual tests.

`StateMachine_Tests<TStateMachine, TSaga>` extends this base to wire production correlation middleware into the test harness. It provides `SeedProduct()` and `SeedInventory()` helper methods that compute deterministic GUIDs from business keys and pre-populate saga instances, ensuring test IDs match the saga instance keys.

**Command service tests** use `CommandService_Tests` with controlled timestamps (`Now` and `Later` constants) and MassTransit inline handlers that simulate state machine responses. These tests verify the service layer's validation, exception translation, and response mapping without running actual state machines.

**Integration tests** use `IntegrationTestBase` with Aspire's `DistributedApplicationTestingBuilder`, spinning up the full AppHost with real RabbitMQ, PostgreSQL, and MongoDB containers via `AppHostFixture`. Tests get `HttpClient` instances through Aspire's service discovery and wait for service health checks before executing.

**Playwright E2E tests** cover the Angular AdminUI with tests for dialog edge cases (rapid click handling, multiple dialog prevention, backdrop management), accessibility compliance, and product deletion flows.

---

## Key Files

- `src/infrastructure/KbStore.AppHost/Program.cs` — Aspire orchestration of all services and infrastructure
- `src/services/KbStore.Catalog/Extensions/HostBuilderExtensions.cs` — MassTransit bus composition
- `src/infrastructure/KbStore.Tests/EventingTestBase.cs` — Test harness with timeout wrapper
- `tests/KbStore.Catalog.Tests/Domains/StateMachine_Tests.cs` — State machine test base with correlation middleware
- `tests/KbStore.IntegrationTests/Infrastructure/IntegrationTestBase.cs` — Full Aspire integration test base
- `docs/PATTERNS.md` — Reference implementations for all platform patterns
