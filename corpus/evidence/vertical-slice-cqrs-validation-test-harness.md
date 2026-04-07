---
title: Vertical Slice CQRS — Test Harness for Vertical Slices
tags: [cqrs, masstransit, testing, nunit, bdd, test-harness, vertical-slice, csharp]
related:
  - evidence/vertical-slice-cqrs-validation.md
  - evidence/vertical-slice-cqrs-validation-structure.md
  - evidence/vertical-slice-cqrs-validation-exception-hierarchy.md
  - evidence/vertical-slice-cqrs-validation-cross-domain-propagation.md
  - evidence/distributed-systems-architecture.md
  - projects/kbstore-ecommerce.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/vertical-slice-cqrs-validation.md
---

# Vertical Slice CQRS — Test Harness for Vertical Slices

The KbStore e-commerce platform (kb-platform) uses an `EventingTestBase` class that boots a MassTransit in-memory test harness with a 250ms request timeout (instead of MassTransit's default 30 seconds). Each command service operation gets its own nested test class following BDD `When_X` naming, and tests distinguish validation failures (message never sent) from domain failures (message sent, handler faulted) using `Harness.Consumed.Any<T>()`.

---

## Evidence: Test Harness for Vertical Slices

The test infrastructure mirrors the vertical slice architecture. `EventingTestBase` (in `src/infrastructure/KbStore.Tests/EventingTestBase.cs`, 223 lines) provides a reusable base that boots a MassTransit in-memory test harness with configurable timeouts:

```csharp
protected static readonly RequestTimeout DefaultRequestTimeout = RequestTimeout.After(ms: 250);
```

Tests use a 250ms request timeout instead of MassTransit's default 30 seconds, ensuring fast feedback when a handler doesn't respond.

Each command service operation gets its own nested test class following BDD `When_X` naming. The `ProductCommandService_Create` test file (`tests/KbStore.Catalog.Tests/Services/Products/ProductCommandService_Create.cs`, 363 lines) contains 11 nested test classes:

- `When_creating_valid_product_without_inventory` — registers a handler, verifies response mapping
- `When_creating_valid_product_with_inventory` — verifies linked inventory correlation
- `When_creating_product_null_sku` / `empty_sku` / `whitespace_sku` — validation guard tests
- `When_creating_product_null_name` / `empty_name` — validation guard tests
- `When_creating_product_invalid_leadtime` — boundary validation
- `When_creating_product_duplicate_sku` — fault reconstitution from `ProductConflictException`
- `When_creating_product_generic_fault` — unmapped fault handling
- `When_creating_product_timeout` — `RequestTimeoutException` handling

Each test verifies three things: the correct exception type was thrown (or not), the result is correctly populated (or null), and whether the MassTransit handler was actually invoked (`Harness.Consumed.Any<CreateProductRequest>()`). This last check distinguishes validation failures (message never sent) from domain failures (message sent, handler faulted).

The test harness uses `conf.AddHandler<T>()` to register inline message handlers that simulate state machine behavior, keeping tests focused on the command service layer without requiring the full saga infrastructure.

Across three domains, approximately 90 test files exist under `tests/KbStore.Catalog.Tests/Services/` and `tests/KbStore.Storefront.Tests/Services/`, each following this same nested-class BDD pattern.

## Key Files

- `kb-platform:src/infrastructure/KbStore.Tests/EventingTestBase.cs` — MassTransit test harness base
- `kb-platform:tests/KbStore.Catalog.Tests/Services/Products/ProductCommandService_Create.cs` — BDD test coverage for command service
