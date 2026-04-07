---
title: Testing Infrastructure — Aspire Integration Tests and Endpoint Test Harness
tags: [testing, aspire, integration-testing, aspnet-core, minimal-apis, nsubstitute, reflection, csharp, kb-platform]
related:
  - evidence/testing-infrastructure.md
  - evidence/testing-infrastructure-base-classes.md
  - evidence/testing-infrastructure-isolation.md
  - evidence/aspnet-minimal-api-patterns.md
  - projects/kbstore-ecommerce.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/testing-infrastructure.md
---

# Testing Infrastructure — Aspire Integration Tests and Endpoint Test Harness

Two higher-level test infrastructure pieces: .NET Aspire integration tests that boot the full distributed application and verify resource registration and health, and a reflection-based endpoint delegate test harness that tests minimal API handlers without spinning up an HTTP pipeline.

---

## Evidence: Aspire Integration Tests

`tests/KbStore.IntegrationTests/` contains integration tests that boot the full .NET Aspire distributed application. `AppHostFixture` (`Infrastructure/AppHostFixture.cs`, 44 lines) uses `DistributedApplicationTestingBuilder.CreateAsync<KbStore.AppHost.Program>()` to spin up the entire orchestration — API service, Catalog domain, Storefront domain, RabbitMQ, PostgreSQL, and MongoDB.

`AspireOrchestrationTests` (`HealthChecks/AspireOrchestrationTests.cs`, 130 lines) validates:
- All 9 expected Aspire resources register (api, domain-catalog, domain-storefront, queue, pgsql, catalog, mongo, storefront, admin-ui)
- RabbitMQ, PostgreSQL, and MongoDB connection strings are accessible
- The API service responds to HTTP requests
- Health check tests exist but are `[Ignore]`d pending backend implementation

`IntegrationTestBase` provides `GetHttpClientAsync(serviceName)` with automatic health polling (up to 30 retries at 1-second intervals) and `GetConnectionStringAsync(databaseName)` for infrastructure tests.

---

## Evidence: Endpoint Test Harness (kb-platform)

`tests/KbStore.ApiService.Tests/Endpoints_Tests.cs` (102 lines) provides a base class for testing ASP.NET Core minimal API endpoint delegates without a full HTTP pipeline. It uses NSubstitute for mocking (`MockOf<TService>()`), reflection-based parameter resolution (`Execute(Delegate handler, params object?[] inputs)`), and the same `Arrange()`/`Act()` pattern with `LastException` capture. The `Execute` method dynamically invokes endpoint delegates by matching registered mocks to parameter types, avoiding the overhead of `WebApplicationFactory` for pure logic tests.

---

## Key Files

- `kb-platform:tests/KbStore.IntegrationTests/Infrastructure/AppHostFixture.cs` — Aspire distributed application test fixture
- `kb-platform:tests/KbStore.IntegrationTests/HealthChecks/AspireOrchestrationTests.cs` — Full-stack resource verification
- `kb-platform:tests/KbStore.ApiService.Tests/Endpoints_Tests.cs` — Reflection-based endpoint delegate test harness
