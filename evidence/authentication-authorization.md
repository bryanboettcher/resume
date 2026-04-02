---
title: Authentication and Authorization Patterns
tags: [authentication, authorization, jwt, api-key, identity, aspnet-core, angular, security, role-based-access, token-management]
related:
  - evidence/aspnet-minimal-api-patterns.md
  - evidence/dependency-injection-composition.md
  - evidence/testing-infrastructure.md
  - projects/call-trader-madera.md
children:
  - evidence/authentication-authorization-multi-scheme.md
  - evidence/authentication-authorization-jwt-tokens.md
  - evidence/authentication-authorization-api-key.md
  - evidence/authentication-authorization-roles-enforcement.md
category: evidence
contact: resume@bryanboettcher.com
---

# Authentication and Authorization Patterns — Index

The madera-apps codebase (Call-Trader direct mail platform, 2024-2025) implements a custom multi-scheme authentication system that supports both interactive JWT-based user sessions and API key-based service account access through a single unified pipeline. Rather than using ASP.NET Identity, the system is built from lower-level ASP.NET Core authentication primitives: custom `AuthenticationHandler<T>` implementations, `IConfigureNamedOptions<JwtBearerOptions>`, policy schemes for request routing, and a server-scoped in-memory token revocation cache. The frontend Angular SPA has a corresponding auth interceptor that handles transparent token refresh with request queuing.

Multiple layers reinforce each other:

- **Scheme forwarding** routes API key requests to a different handler without endpoint-level branching
- **Browser fingerprinting** ties tokens to a specific User-Agent + server instance
- **Server-restart invalidation** via `HashPepper` in both the signing key and the fingerprint acts as a global token revocation on deploy
- **Claim-based refresh gating** prevents refresh tokens from being used as access tokens
- **Session binding** via database-stored session IDs means a second login implicitly invalidates the first device's refresh chain
- **Lazy-prune revocation** keeps the revocation cache bounded without a background timer
- **Middleware exception handling** ensures token validation failures always result in revocation, not just rejection
- **Reflection-based test enforcement** guarantees authorization coverage across all discovered endpoints

The full evidence is split into focused documents:

## Child Documents

- **[Multi-Scheme Setup and Default Policy](authentication-authorization-multi-scheme.md)** — How three authentication schemes (JWT bearer, API key, policy forwarding) are wired up. The `AddApiKey()` extension method. The default authorization policy's `RequireClaim(CHash)` constraint that prevents refresh tokens from being used as access tokens.

- **[JWT Token Service, Revocation, and Refresh](authentication-authorization-jwt-tokens.md)** — JWT creation with browser fingerprinting (`c_hash` claim). The refresh flow: revocation check, browser fingerprint re-validation, session binding via database `sid`. The `ServerLifetimeCache` static in-memory revocation store with lazy TTL pruning and `HashPepper`. The custom lifetime validator and `TokenSecurityMiddleware` that revokes tokens on validation failure.

- **[API Key Handler and BCrypt Password Hashing](authentication-authorization-api-key.md)** — The `ApiKeyAuthenticationScheme` `AuthenticationHandler<T>` with three-tier response pattern (NoResult / BadRequest / Fail). The `LegacyPasswordHasher` BCrypt adapter for the pre-existing password store.

- **[Role-Based Access and Angular Interceptor](authentication-authorization-roles-enforcement.md)** — Dynamic role assembly from database rows plus computed flags (`active`, `inactive`, `admin`, `pw_reset`). Role-gated endpoint with ownership enforcement. The Angular `BehaviorSubject`-based token refresh interceptor with thundering-herd prevention. Reflection-based compile-time test that fails the build if any `IEndpoint` lacks `[Authorize]`.
