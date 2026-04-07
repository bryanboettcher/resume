---
title: Angular Service Architecture Patterns
tags: [angular, typescript, rxjs, dependency-injection, caching, generics, http, testing]
related:
  - evidence/frontend-web-development.md
  - projects/call-trader-madera.md
children:
  - evidence/angular-service-patterns-rest-repository.md
  - evidence/angular-service-patterns-facade-service.md
  - evidence/angular-service-patterns-caching-dropdown.md
  - evidence/angular-service-patterns-string-matching.md
  - evidence/angular-service-patterns-observable-composition.md
  - evidence/angular-service-patterns-auth-lifecycle.md
category: evidence
contact: resume@bryanboettcher.com
---

# Angular Service Architecture Patterns — Index

The Madera direct mail platform (`madera-apps`) includes an Angular frontend with a well-structured service layer spanning 15+ service files. Rather than scattering HTTP calls across components, the codebase uses a generic repository base class, typed pagination contracts mirrored from the .NET backend, an interface-driven caching dropdown service, a strategy-pattern string matching service, `switchMap`-composed multi-step HTTP operations, saga state guards at the service layer, and a split three-service auth lifecycle.

## Child Documents

- **[Generic REST Repository Base Class](angular-service-patterns-rest-repository.md)** — `RestRepository<TKey, TModel extends IKeyed<TKey>>` with standard CRUD, automatic JSON/FormData switching on file detection, nested repo support for hierarchical resources, and a `PaginatedResult<T>` contract that mirrors the server-side C# type. Plus `ReportRepository<TModel, TParams>` for the separate `/reports/` namespace.

- **[Facade Service Composing Multiple Repositories](angular-service-patterns-facade-service.md)** — `DirectMailService` (223 lines) composes 8 private repository classes (each a 2-line class setting an API fragment) behind a single injection point. Delegates to repositories for CRUD and uses direct `HttpClient` for RPC-style endpoints — a deliberate pragmatic split.

- **[Caching Dropdown Service with Interface Abstraction](angular-service-patterns-caching-dropdown.md)** — `IDropdownService` / `CacheDropdownService` with URL-keyed Map cache, RxJS `tap`-based cache population, `of()` short-circuit on hits, and response shape normalization (`name` → `value`). Tested with 239-line `HttpTestingController` suite verifying cache hit/miss/isolation/failure behavior.

- **[Strategy Pattern for String Matching](angular-service-patterns-string-matching.md)** — `IMatchingService` / `LevenshteinDistanceMatchingService` for CSV import column fuzzy-matching. Full dynamic programming DP algorithm, 50%-of-word-length match threshold, used to auto-suggest column mappings from uploaded CSV headers.

- **[Observable Composition and State-Guarded Operations](angular-service-patterns-observable-composition.md)** — `switchMap` chains for two-step import creation (POST metadata → upload file with correlation ID). `restrictToStates()` guard reflecting backend saga state machine: invalid operations surface a toast rather than making the API call.

- **[Auth Token Lifecycle Management](angular-service-patterns-auth-lifecycle.md)** — Three-service split: `TokenService` (JWT storage/expiration), `AuthService` (higher-level session ops), `NavigationService` (post-login URL restore). `clearSession()` preserves navigation state across `localStorage.clear()` to prevent losing the user's place on logout.
