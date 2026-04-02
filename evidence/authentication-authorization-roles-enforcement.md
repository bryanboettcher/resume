---
title: Authentication and Authorization — Role-Based Access and Angular Interceptor
tags: [authentication, authorization, role-based-access, angular, aspnet-core, compile-time-enforcement, token-refresh, rxjs, reflection]
related:
  - evidence/authentication-authorization.md
  - evidence/authentication-authorization-multi-scheme.md
  - evidence/authentication-authorization-jwt-tokens.md
  - evidence/authentication-authorization-api-key.md
  - evidence/aspnet-minimal-api-patterns.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/authentication-authorization.md
---

# Authentication and Authorization — Role-Based Access and Angular Interceptor

This document covers the role assembly from database and computed flags, the frontend Angular interceptor with transparent token refresh, and the reflection-based test that enforces `[Authorize]` coverage across all endpoints at compile time.

---

## Evidence: Role-Based Authorization

The `SqlAccountService.BuildMaderaAccount` method constructs the role list dynamically from database roles plus computed flags:

```csharp
var rolesList = (await GetRoles(user.Id, token)).ToList();

rolesList.Add("user");

if (user.IsActive)
    rolesList.Add("active");

if (!user.IsActive)
    rolesList.Add("inactive");

if (user.IsAdmin)
    rolesList.Add("admin");

if (user.NeedsReset)
    rolesList.Add("pw_reset");
```

These roles feed directly into JWT claims and are consumed by `[Authorize(Roles = "...")]` attributes on endpoints. The `UpdatePassword` endpoint in `AuthenticationEndpoints` shows role-based logic: it requires either `admin` or `pw_reset` role, and enforces that non-admin users can only change their own password:

```csharp
[Authorize(Roles = "admin,pw_reset")]
public static async Task<IResult> UpdatePassword(
    [FromBody] PasswordUpdateRequest payload,
    [FromServices] IUserAccountService userAccountService,
    [FromServices] ClaimsPrincipal currentUser,
    CancellationToken token)
{
    var isAdmin = currentUser.IsInRole("admin");
    if (!isAdmin && payload.UserId != currentUserId)
    {
        return Results.Problem(new ProblemDetails
        {
            Detail = $"Current user does not have permission to set password for userId {payload.UserId}",
            Status = (int)HttpStatusCode.Forbidden,
        });
    }
    // ...
}
```

---

## Evidence: Angular Auth Interceptor with Transparent Refresh

The frontend auth interceptor (`Madera/madera.ui.client/src/app/interceptors/auth.interceptor.ts`) implements transparent token refresh with request queuing. When a 401 response arrives, the interceptor:

1. Checks if a refresh is already in progress. If so, queues the request by subscribing to a `BehaviorSubject<string | null>` that will emit the new token when the refresh completes
2. If no refresh is in progress, sets a flag, calls `authService.refreshAuth()`, and on success emits the new token to the subject (unblocking all queued requests)
3. On refresh failure, logs the user out and clears the token subject

This avoids the thundering-herd problem where multiple concurrent 401s trigger multiple refresh calls. Only the first 401 initiates the refresh; subsequent requests queue behind the `BehaviorSubject` filter:

```typescript
if (refreshTokenInProgress) {
    return refreshTokenSubject.pipe(
        filter(token => token !== null),
        take(1),
        switchMap(token => {
            const requestWithNewToken = req.clone({
                setHeaders: { Authorization: `Bearer ${token}` }
            });
            return next(requestWithNewToken);
        })
    );
}
```

---

## Evidence: Compile-Time Authorization Enforcement

The `EndpointAuthTests.All_endpoints_follow_standards` test (`Madera/Madera.UI.Server.Tests/EndpointAuthTests.cs`) uses reflection to scan every `IEndpoint` implementation in the assembly and verify structural contracts, including that every endpoint handler has an `[Authorize]` attribute:

```csharp
var authorizeAttribute = handler.GetCustomAttribute<AuthorizeAttribute>();
authorizeAttribute.ShouldNotBeNull(
    $"all endpoints must have an [Authorize] attribute, but {endpoint.Name} does not");
```

This means adding a new endpoint without `[Authorize]` fails the build. The authentication endpoints (`/auth/login`, `/auth/refresh`) are registered separately via `MapAuthenticationEndpoints` and use `[AllowAnonymous]`, so they are not subject to this test (they are not `IEndpoint` implementations).

---

## Key Files

- `madera-apps:Madera/Madera.UI.Server/Identity/Services/SqlAccountService.cs` — Dapper-based account service with session ID generation and role assembly
- `madera-apps:Madera/Madera.UI.Server/Identity/AuthenticationEndpoints.cs` — Login, refresh, and password change endpoints
- `madera-apps:Madera/madera.ui.client/src/app/interceptors/auth.interceptor.ts` — Angular HTTP interceptor with transparent refresh and request queuing
- `madera-apps:Madera/madera.ui.client/src/app/services/auth.service.ts` — Auth service with session check and logout
- `madera-apps:Madera/Madera.UI.Server.Tests/EndpointAuthTests.cs` — Reflection-based [Authorize] enforcement across all endpoints
