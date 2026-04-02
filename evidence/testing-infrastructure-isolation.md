---
title: Testing Infrastructure — Test Isolation and DI/Consumer Registration Tests
tags: [testing, nunit, deterministic-guid, saga-seeding, test-isolation, dependency-injection, masstransit, consumer-registration, aspnet-core, csharp, kb-platform]
related:
  - evidence/testing-infrastructure.md
  - evidence/testing-infrastructure-base-classes.md
  - evidence/testing-infrastructure-bdd-organization.md
  - projects/kbstore-ecommerce.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/testing-infrastructure.md
---

# Testing Infrastructure — Test Isolation and DI/Consumer Registration Tests

Two complementary infrastructure concerns: deterministic GUID namespace rotation prevents saga ID collisions between test runs, and DI/consumer registration tests catch wiring regressions by booting the actual production DI container and verifying all expected services and MassTransit consumers resolve.

---

## Evidence: Test Isolation Infrastructure

### Deterministic GUID Namespace Rotation

`tests/KbStore.Catalog.Tests/GlobalTestSetup.cs` (20 lines) uses NUnit's `[SetUpFixture]` to set a test-run-specific namespace for deterministic GUID generation:

```csharp
var testNamespace = (int)(DateTime.UtcNow.Ticks & 0xFFFFFFFF);
DeterministicGuid.DefaultNamespace = testNamespace;
```

Since saga correlation IDs are computed deterministically from business identifiers (e.g., `DeterministicGuid.FromProductSku("TEST_SKU_123")`), rotating the namespace per test run prevents saga collisions when the in-memory repository persists across tests in the same fixture.

### Saga Seeding Helpers

`StateMachine_Tests` provides `SeedProduct()` and `SeedInventory()` that compute the deterministic GUID and call `Harness.AddOrUpdateSagaInstance<T>()`. This ensures the seeded CorrelationId matches the ID the middleware would compute from the same business identifier — preventing a category of test bugs where hardcoded GUIDs drift from the correlation logic.

---

## Evidence: DI Container and Consumer Registration Tests

`tests/KbStore.ApiService.Tests/Infrastructure/DependencyInjectionTests.cs` (120 lines) boots the actual `WebApplication.CreateBuilder()` with in-memory configuration, calls the production `AddApplicationServices()` extension method, and verifies services resolve from the container. This catches DI registration regressions without requiring live infrastructure.

`MassTransitConfigurationTests.cs` (162 lines) goes further: it verifies that all 10 expected MassTransit event consumers in the API gateway are auto-discovered and resolvable. It uses assembly reflection to find all types under `KbStore.ApiService.Consumers`, then resolves each from the DI container. This catches the common problem where a consumer class exists but is not registered with MassTransit.

---

## Key Files

- `kb-platform:tests/KbStore.Catalog.Tests/GlobalTestSetup.cs` — Deterministic GUID namespace rotation for test isolation
- `kb-platform:tests/KbStore.ApiService.Tests/Infrastructure/DependencyInjectionTests.cs` — DI container verification
- `kb-platform:tests/KbStore.ApiService.Tests/Infrastructure/MassTransitConfigurationTests.cs` — Consumer registration verification
