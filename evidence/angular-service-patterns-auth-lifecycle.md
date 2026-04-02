---
title: Angular Service Patterns — Auth Token Lifecycle Management
tags: [angular, typescript, jwt, authentication, local-storage, session-management, rxjs]
related:
  - evidence/angular-service-patterns.md
  - evidence/angular-service-patterns-observable-composition.md
  - evidence/angular-service-patterns-rest-repository.md
  - evidence/frontend-web-development.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/angular-service-patterns.md
---

# Angular Service Patterns — Auth Token Lifecycle Management

Authentication in the Madera Angular frontend is split across three focused services: `TokenService` manages JWT storage and expiration checks, `AuthService` composes `TokenService` and `NavigationService` for higher-level operations, and `NavigationService` handles post-login URL restoration. A notable detail: `clearSession()` preserves `previousUrl` and `previousParams` before clearing localStorage and restores them after — preventing the user from losing their place in the application on logout.

---

## Evidence: Auth Token Lifecycle Management

**Files:**
- `Madera/madera.ui.client/src/app/services/token.service.ts`
- `Madera/madera.ui.client/src/app/services/auth.service.ts`
- `Madera/madera.ui.client/src/app/services/navigation.service.ts`

Authentication is split across three focused services. `TokenService` manages JWT storage in `localStorage`, expiration checks (both access and refresh tokens), and token refresh via HTTP POST. `AuthService` composes `TokenService` and `NavigationService` to provide higher-level operations: `sessionActive()`, `verifyLogin()`, `logout()`, and `refreshAuth()`. `NavigationService` handles post-login redirect, restoring the user's previous URL and query parameters from `localStorage`.

The `clearSession()` method in `AuthService` preserves navigation state during logout — it saves `previousUrl` and `previousParams` before calling `localStorage.clear()`, then restores them. This means logging out doesn't lose the user's place in the application. The inline comment (`// gross`) suggests this is a known pragmatic workaround rather than an ideal solution.

## Key Files

- `madera-apps:Madera/madera.ui.client/src/app/services/token.service.ts` — JWT lifecycle management
- `madera-apps:Madera/madera.ui.client/src/app/services/auth.service.ts` — Auth facade over token + navigation
