---
title: Testing Infrastructure Across the Stack
tags: [testing, nunit, shouldly, masstransit, playwright, bdd, test-harness, integration-testing, e2e, angular, aspire, csharp, typescript]
related:
  - evidence/vertical-slice-cqrs-validation-test-harness.md
  - evidence/vertical-slice-cqrs-validation.md
  - evidence/distributed-systems-architecture.md
  - projects/kbstore-ecommerce.md
  - projects/call-trader-madera.md
children:
  - evidence/testing-infrastructure-base-classes.md
  - evidence/testing-infrastructure-bdd-organization.md
  - evidence/testing-infrastructure-isolation.md
  - evidence/testing-infrastructure-aspire-integration.md
  - evidence/testing-infrastructure-playwright-angular.md
category: evidence
contact: resume@bryanboettcher.com
---

# Testing Infrastructure Across the Stack — Index

Bryan builds layered testing infrastructure that scales across projects. Across two major repositories (kb-platform and madera-apps), there are approximately 295 test files totaling nearly 25,000 lines of test code in C# and TypeScript. Rather than treating tests as an afterthought, the codebases show deliberate investment in reusable test base classes, BDD organizational patterns, and test harnesses purpose-built for asynchronous message-driven architectures.

## Testing Stack Summary

| Layer | Framework | Assertion Library | Repo |
|-------|-----------|-------------------|------|
| Saga state machine tests | NUnit + MassTransit TestHarness | Shouldly | kb-platform |
| Command service tests | NUnit + MassTransit TestHarness | Shouldly | kb-platform |
| Endpoint delegate tests | NUnit + NSubstitute | Shouldly | kb-platform |
| DI/consumer registration tests | NUnit | Shouldly | kb-platform |
| Aspire integration tests | NUnit + Aspire Testing | Shouldly | kb-platform |
| Address saga tests | NUnit + MassTransit TestHarness + Moq | NUnit Assert | madera-apps |
| Unit tests (service layer) | NUnit + Moq.AutoMock | NUnit Assert | madera-apps |
| Angular component/service tests | Jest + TestBed | Jest matchers | kb-platform, madera-apps |
| E2E tests | Playwright | Playwright expect | kb-platform |
| Address pipeline tests | NUnit | NUnit Assert | FastAddress |

The full evidence is split into focused documents:

## Child Documents

- **[Test Base Class Hierarchy](testing-infrastructure-base-classes.md)** — `EventingTestBase` (223 lines) with 250ms fast-failure timeout decorator, template method lifecycle, and virtual extension points. `StateMachine_Tests` and `CommandService_Tests` subclasses. Madera's `MassTransitTestBase` with `ISystemClock` mock. `WithFakes`/`WithSubject<T>` pattern with Moq.AutoMock for non-harness unit tests.

- **[BDD Test Organization](testing-infrastructure-bdd-organization.md)** — Nested class pattern (`When_creating_with_inventory_link_faulted : Product_Create`) with one `[Test]` method per scenario named `It_should_be_correct`. Three-way assertions: exception type, result population, and message bus activity. Inline handler registration for fault simulation (success, domain fault, timeout).

- **[Test Isolation and DI/Consumer Registration Tests](testing-infrastructure-isolation.md)** — Deterministic GUID namespace rotation via `[SetUpFixture]` to prevent saga ID collisions between runs. Saga seeding helpers that compute the same deterministic ID the production middleware would use. DI and MassTransit consumer registration tests that boot the production container and verify all 10 expected consumers resolve.

- **[Aspire Integration Tests and Endpoint Test Harness](testing-infrastructure-aspire-integration.md)** — `AppHostFixture` using `DistributedApplicationTestingBuilder` to boot the full orchestration (9 resources) with health polling. `Endpoints_Tests` reflection-based harness that invokes endpoint delegates by matching mocks to parameter types, without `WebApplicationFactory`.

- **[Playwright E2E Tests and Angular Unit Tests](testing-infrastructure-playwright-angular.md)** — Three Playwright test files (267, 387, 391 lines) covering product delete workflow, ARIA/keyboard accessibility (focus trapping, Tab cycling, computed style focus indicators), and race condition/boundary edge cases. Jest-based Angular unit tests up to 851 lines.
