---
title: Testing Infrastructure — BDD Test Organization
tags: [testing, bdd, nunit, nested-classes, shouldly, masstransit, inline-handlers, fault-simulation, csharp, kb-platform]
related:
  - evidence/testing-infrastructure.md
  - evidence/testing-infrastructure-base-classes.md
  - evidence/testing-infrastructure-isolation.md
  - projects/kbstore-ecommerce.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/testing-infrastructure.md
---

# Testing Infrastructure — BDD Test Organization

Both kb-platform and madera-apps use nested class hierarchies for BDD-style test organization. A parent class sets up shared context; nested classes each represent a distinct scenario, overriding `Arrange()`, `Act()`, or `OnHarnessCreating()` to configure their specific conditions.

---

## Evidence: BDD Test Organization

### Nested Class Pattern

Both repositories use nested classes for BDD-style test organization. A test file like `Product_Create.cs` (`tests/KbStore.Catalog.Tests/Domains/Products/Product_Create.cs`, 274 lines) follows this structure:

```
Product_Create : StateMachine_Tests<ProductStateMachine, ProductEntity>
    When_creating_without_inventory_link : Product_Create
    When_creating_with_inventory_link_successful : Product_Create
    When_creating_with_inventory_link_faulted : Product_Create
    When_creating_with_inventory_link_timeout : Product_Create
    When_creating_with_duplicate_sku : Product_Create
```

Each nested class inherits from the parent, overriding `Arrange()` or `Act()` or `OnHarnessCreating()` to set up its specific scenario. The parent class provides the shared setup (creating a request client, defining the default message). Each nested class provides one `[Test]` method named `It_should_be_correct`.

This creates readable test output: `Product_Create / When_creating_with_inventory_link_faulted / It_should_be_correct`.

### Three-Way Test Assertions

The command service tests use a consistent three-way assertion pattern:
1. **Exception type** — `LastException.ShouldBeNull()` for success, `LastException.ShouldBeOfType<ProductValidationException>()` for validation failures, `LastException.ShouldBeOfType<RequestTimeoutException>()` for timeouts
2. **Result population** — `Result.ShouldNotBeNull()` with `ShouldSatisfyAllConditions()` for success, `Result.ShouldBeNull()` for failures
3. **Message bus activity** — `(await Harness.Consumed.Any<CreateProductRequest>()).ShouldBeFalse()` for validation failures (message never sent), `.ShouldBeTrue()` for domain failures and successes (message sent)

This third check is the key insight: it distinguishes between failures that happen before the message boundary (validation) and failures that happen after (domain logic, timeouts, faults). This is important for message-driven architectures where the distinction determines retry behavior.

### Inline Handler Registration for Fault Simulation

Tests simulate different failure modes by registering inline handlers in `OnHarnessCreating`:
- **Successful response**: `conf.AddHandler<Request>(async ctx => await ctx.RespondAsync<Response>(new { ... }))`
- **Domain fault**: `conf.AddHandler<Request>(ctx => throw ProductConflictException.DuplicateSku(...))`
- **Timeout**: `conf.AddHandler<Request>(async ctx => await Task.Delay(2000, ctx.CancellationToken))`

The test harness's 250ms timeout causes the delay handler to trigger a `RequestTimeoutException` naturally, rather than artificially constructing one.

---

## Key Files

- `kb-platform:tests/KbStore.Catalog.Tests/Domains/Products/Product_Create.cs` — BDD nested-class saga tests (5 scenarios)
- `kb-platform:tests/KbStore.Catalog.Tests/Services/Products/ProductCommandService_Create.cs` — BDD nested-class service tests (11 scenarios)
- `madera-apps:Madera/Madera.Dataflows.DirectMail.Tests/Address_Tests.cs` — Address saga BDD tests (nested When_ classes)
