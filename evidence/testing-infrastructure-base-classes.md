---
title: Testing Infrastructure — Test Base Class Hierarchy
tags: [testing, nunit, masstransit, test-harness, base-class, moq, automock, csharp, kb-platform, madera-apps]
related:
  - evidence/testing-infrastructure.md
  - evidence/testing-infrastructure-bdd-organization.md
  - evidence/testing-infrastructure-isolation.md
  - evidence/distributed-systems-architecture.md
  - projects/kbstore-ecommerce.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/testing-infrastructure.md
---

# Testing Infrastructure — Test Base Class Hierarchy

Across kb-platform and madera-apps, reusable test base classes eliminate harness wiring boilerplate and establish consistent lifecycle management. The foundation is MassTransit in-memory test harnesses with fast-failure timeouts, template method lifecycle, and auto-mock subject construction.

---

## Evidence: Test Base Class Hierarchy

### EventingTestBase (kb-platform)

The foundation of kb-platform's backend testing is `EventingTestBase` in `src/infrastructure/KbStore.Tests/EventingTestBase.cs` (223 lines). This abstract base class manages the full lifecycle of a MassTransit in-memory test harness with several deliberate design choices:

**Fast-failure timeouts.** MassTransit's default request timeout is 30 seconds. In tests, a missing or broken handler that silently hangs for 30 seconds makes debugging painful. `EventingTestBase` sets a 250ms request timeout via a custom `TimeoutRequestClient<TRequest>` decorator class. This decorator wraps `IRequestClient<TRequest>` and injects the 250ms default into every `GetResponse` overload (all 12 of them, covering 1-3 response types with and without `RequestPipeConfiguratorCallback`). Tests fail in under a second when a handler is missing.

**Template Method lifecycle.** The base class follows a strict lifecycle using NUnit's `[OneTimeSetUp]` / `[SetUp]` / `[TearDown]` / `[OneTimeTearDown]` attributes:
- `OneTimeSetUp`: builds the DI container, registers the MassTransit test harness, starts it
- `SetUp`: creates a scoped provider, calls `Arrange()`, then calls `Act()` with exception capture into `LastException`
- `TearDown`: disposes the scoped provider
- `OneTimeTearDown`: stops the harness, async-disposes everything

Subclasses override `Arrange()` and `Act()` — the base class handles exception capture so every test can inspect `LastException` without try/catch boilerplate.

**Extension points.** Virtual methods `OnServicesCreating(IServiceCollection)` and `OnHarnessCreating(IBusRegistrationConfigurator)` let subclasses register domain-specific services and MassTransit consumers/sagas without touching the harness wiring.

### StateMachine_Tests (kb-platform)

`tests/KbStore.Catalog.Tests/Domains/StateMachine_Tests.cs` extends `EventingTestBase` for saga state machine testing. It:
- Registers a saga state machine with an in-memory repository via `configurator.AddSagaStateMachine<TStateMachine, TSaga>().InMemoryRepository()`
- Wires production correlation middleware (`ProductCorrelationMiddleware<>`, `InventoryCorrelationMiddleware<>`) into the in-memory bus — tests run the same middleware pipeline as production
- Provides `SeedProduct(string sku, Action<ProductEntity> configure)` and `SeedInventory(string partNumber, Action<InventoryEntity> configure)` helper methods that pre-populate saga instances using deterministic GUIDs computed from business identifiers

### CommandService_Tests (kb-platform)

`tests/KbStore.Catalog.Tests/Services/CommandService_Tests.cs` extends `EventingTestBase` for command service layer testing. It registers the service-under-test as a transient in the DI container and resolves it from the scoped provider in `Arrange()`. Tests at this layer register inline MassTransit handlers via `conf.AddHandler<T>()` to simulate state machine responses, keeping focus on the command service validation and mapping logic without the full saga infrastructure.

### MassTransitTestBase (madera-apps)

The madera-apps codebase has its own MassTransit test harness base in `Madera/Madera.Dataflows.DirectMail.Tests/MassTransitTestBase.cs` (63 lines). Same pattern — builds a `ServiceCollection`, registers `AddMassTransitTestHarness`, exposes `ITestHarness` — but with a `Mock<ISystemClock>` for time-dependent tests. This base supports the address state machine saga tests in the direct mail dataflow.

### WithFakes / WithSubject (madera-apps)

For non-MassTransit unit tests, madera-apps uses a two-level base class hierarchy:

- `WithFakes` (`Madera/Madera.Common.Tests/WithFakes.cs`): provides `An<T>()` for creating Moq mock instances and a `Catch(Action)` helper for exception capture
- `WithSubject<TSubject>` (`Madera/Madera.Common.Tests/WithSubject.cs`): extends `WithFakes`, adds `AutoMocker` from Moq.AutoMock to auto-fill constructor dependencies. Provides `Subject` (auto-constructed instance) and `The<T>()` (retrieve a specific mock for setup/verification)

This pattern eliminates manual mock wiring. Test classes declare `class MyTests : WithSubject<MyService>` and get a fully-constructed subject with all dependencies mocked.

---

## Key Files

- `kb-platform:src/infrastructure/KbStore.Tests/EventingTestBase.cs` — MassTransit test harness base with 250ms timeout decorator
- `kb-platform:tests/KbStore.Catalog.Tests/Domains/StateMachine_Tests.cs` — Saga test base with middleware wiring and seed helpers
- `kb-platform:tests/KbStore.Catalog.Tests/Services/CommandService_Tests.cs` — Command service test base with DI resolution
- `madera-apps:Madera/Madera.Dataflows.DirectMail.Tests/MassTransitTestBase.cs` — Madera MassTransit test harness with ISystemClock mock
- `madera-apps:Madera/Madera.Common.Tests/WithFakes.cs` — Mock helper base class
- `madera-apps:Madera/Madera.Common.Tests/WithSubject.cs` — AutoMocker-based subject construction base
