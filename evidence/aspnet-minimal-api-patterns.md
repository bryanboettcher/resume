---
title: ASP.NET Core Minimal API Endpoint Design Patterns
tags: [aspnet-core, minimal-apis, rest, cqrs, masstransit, fluentvalidation, endpoint-routing, pagination, authorization, vertical-slice]
related:
  - evidence/vertical-slice-cqrs-validation.md
  - evidence/dotnet-csharp-expertise.md
  - evidence/masstransit-contract-design.md
  - projects/kbstore-ecommerce.md
  - projects/call-trader-madera.md
children:
  - evidence/aspnet-minimal-api-patterns-static-endpoints.md
  - evidence/aspnet-minimal-api-patterns-reflection-discovery.md
  - evidence/aspnet-minimal-api-patterns-technical-details.md
category: evidence
contact: resume@bryanboettcher.com
---

# ASP.NET Core Minimal API Endpoint Design Patterns — Index

Two production codebases — kb-platform (e-commerce) and madera-apps (direct mail platform) — both use ASP.NET Core minimal API endpoints as their HTTP layer, but with different organizational strategies reflecting different project needs. kb-platform uses a static-class-with-MapTo approach where each bounded context groups its endpoints into a single file. madera-apps uses a one-class-per-endpoint convention with reflection-based discovery that auto-wires every endpoint at startup.

Both converge on the same core shape: static handler methods, parameter binding via attributes, `CancellationToken` propagation, and `IResult` return types. The difference in organization reveals a deliberate choice about team scale and operational domain complexity.

The full evidence is split into focused documents:

## Child Documents

- **[Static Endpoint Classes with MapTo (kb-platform)](aspnet-minimal-api-patterns-static-endpoints.md)** — `SellableItemEndpoints` (435 lines, 17 handlers) as the archetypal example. Hierarchical route group composition via `MapApplicationEndpoints`. CQRS visible at the endpoint level: command services vs. query services. State transitions as PATCH sub-resources rather than generic PUT. Direct service injection — the message bus boundary is behind the service abstraction.

- **[One-Class-Per-Endpoint with Reflection Discovery (madera-apps)](aspnet-minimal-api-patterns-reflection-discovery.md)** — Marker interface hierarchy (`IRestEndpoint`, `IRpcEndpoint`, `IReportEndpoint`). Each endpoint file contains the handler class, payload DTO, and FluentValidation validator. `MapDiscoveredEndpoints` using `Delegate.CreateDelegate` for auto-registration with duplicate route detection. MassTransit `IRequestClient<TCommand>` as the HTTP-to-backend bridge with typed failure `AsResult()` mapping. Fire-and-forget via `IPublishEndpoint`.

- **[Validation, Pagination, Auth Enforcement, and OpenAPI](aspnet-minimal-api-patterns-technical-details.md)** — Hand-rolled `IsValid()` (kb-platform) vs FluentValidation with co-located validators (madera-apps). Custom `PaginationRequest.BindAsync` with page size allowlist. Reflection-based `EndpointAuthTests` that scans the entire assembly and fails the build on convention violations (`[Authorize]`, binding attributes, `CancellationToken` last). OpenAPI via `[Tags]` and `.WithOpenApi()`.
