---
title: Authentication and Authorization — JWT Token Service, Revocation, and Refresh
tags: [authentication, jwt, token-management, browser-fingerprinting, revocation, refresh-token, session-binding, security, aspnet-core]
related:
  - evidence/authentication-authorization.md
  - evidence/authentication-authorization-multi-scheme.md
  - evidence/authentication-authorization-api-key.md
  - evidence/authentication-authorization-roles-enforcement.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/authentication-authorization.md
---

# Authentication and Authorization — JWT Token Service, Revocation, and Refresh

The madera-apps JWT implementation builds tokens with browser fingerprinting, uses an in-memory revocation cache tied to application lifetime, and implements a token refresh flow that validates session binding before issuing new tokens.

---

## Evidence: JWT Token Service with Browser Fingerprinting

The `DefaultTokenService` class (277 lines, `Madera/Madera.UI.Server/Identity/JwtScheme/DefaultTokenService.cs`) handles both token creation and refresh validation. Token creation builds two separate JWTs — an access token and a refresh token — with different claim sets and lifetimes.

The access token includes standard registered claims (jti, sid, iss, aud, sub, exp, iat, nbf), user profile claims (given_name, family_name, email), role claims, and a `c_hash` claim containing a browser fingerprint:

```csharp
private string BuildBrowserHash()
{
    var userAgent = _context.Request.Headers.UserAgent.ToString();
    var hashPayload = Encoding.UTF8.GetBytes(
        string.Concat(
            userAgent,
            ServerLifetimeCache.HashPepper.ToString("N")
        )
    );

    return Convert.ToBase64String(MD5.HashData(hashPayload));
}
```

The fingerprint combines the User-Agent header with a `HashPepper` — a `Guid.NewGuid()` generated once at application startup in `ServerLifetimeCache`. This means the fingerprint is specific to the combination of client browser and server instance lifetime. If the server restarts, the pepper changes and all existing tokens' fingerprints become invalid. If a token is replayed from a different browser, the fingerprint won't match.

The refresh token is deliberately minimal — it contains only `sub`, `sid`, and `exp`. No browser fingerprint, no roles, no profile data. This is what makes the `RequireClaim(CHash)` authorization policy effective as a gating mechanism.

---

## Evidence: Token Refresh with Session Binding and Revocation

The `GetRefreshPayload` method handles the refresh flow. It validates the expired access token (with lifetime validation disabled) and the refresh token (with lifetime validation enabled), then performs several security checks:

1. **Revocation check**: Verifies the access token's JTI has not been revoked via `ServerLifetimeCache.IsRevoked()`
2. **Browser match**: Recomputes the browser fingerprint and compares it against the `c_hash` claim in the access token. If they don't match, the token is revoked and a `TokenException` is thrown
3. **Session extraction**: Pulls the user ID from the refresh token's `sub` claim and session ID from `sid`
4. **Pre-emptive revocation**: If the access token hasn't expired yet (early refresh), the old token is added to the revocation cache

The session ID (`sid`) ties the refresh chain to a specific database session. The `SqlAccountService.BuildMaderaAccount` method generates a new `Guid` session ID on every login and writes it to the database:

```csharp
private async Task<MaderaAccount?> BuildMaderaAccount(
    DatabaseUser user,
    MaderaAccount account,
    CancellationToken token)
{
    var sessionId = Guid.NewGuid();
    await SetUserSessionId(user.Id, sessionId);
    account.TokenSessionId = sessionId;
    // ...
}
```

This means logging in from a second device generates a new session ID, and the next refresh attempt from the first device will fail because the session ID in the refresh token no longer matches the database.

---

## Evidence: In-Memory Token Revocation Cache

`ServerLifetimeCache` (`Madera/Madera.UI.Server/BackgroundServices/ServerLifetimeCache.cs`) is a static in-memory cache that tracks revoked token IDs with expiration-based pruning:

```csharp
public static class ServerLifetimeCache
{
    static ServerLifetimeCache()
    {
        HashPepper = Guid.NewGuid();
    }

    public static Guid HashPepper { get; }

    private static readonly Dictionary<Guid, DateTimeOffset> _revokedTokenIds = new();

    public static bool IsRevoked(Guid tokenId)
    {
        Prune();
        return _revokedTokenIds.ContainsKey(tokenId);
    }

    public static void Revoke(Guid tokenId, DateTimeOffset expiration)
    {
        Prune();
        _revokedTokenIds[tokenId] = expiration;
    }

    private static void Prune()
    {
        var now = DateTimeOffset.UtcNow;
        var expired = _revokedTokenIds
                     .Where(kvp => kvp.Value < now)
                     .Select(kvp => kvp.Key)
                     .ToList();
        expired.ForEach(key => _revokedTokenIds.Remove(key));
    }
}
```

The pruning strategy is lazy — it runs on every `IsRevoked` or `Revoke` call, removing entries whose expiration has passed. The revocation TTL is set to `AllowedClockSkewMinutes + RefreshExpirationMinutes`, meaning tokens stay in the revocation set long enough to cover any valid refresh window plus clock drift.

This cache also serves double duty: its `HashPepper` property (a `Guid` generated once in the static constructor) is concatenated into both the browser fingerprint hash and the JWT signing key via `JwtSettings.SecurityKey`:

```csharp
public SecurityKey SecurityKey => new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(
        string.Concat(
            Key,
            ServerLifetimeCache.HashPepper.ToString("N")
        )
    )
);
```

This means every server restart generates a new signing key suffix, invalidating all previously issued tokens. It is a deliberate trade-off for a single-instance deployment: simpler than distributed revocation, with the cost that a deploy bounces all user sessions.

---

## Evidence: Custom JWT Bearer Options Configuration and Token Security Middleware

`ConfigureJwtBearerOptions` implements `IConfigureNamedOptions<JwtBearerOptions>` and hooks into the validation pipeline with a custom `LifetimeValidator` that checks the revocation cache before delegating to standard lifetime validation:

```csharp
private bool LifetimeValidator(
    DateTime? notBefore,
    DateTime? expires,
    SecurityToken securityToken,
    TokenValidationParameters validationParameters)
{
    var tokenId = Guid.Parse(securityToken.Id);

    if (ServerLifetimeCache.IsRevoked(tokenId))
        throw new SecurityTokenInvalidLifetimeException("Token ID has been revoked");

    var clonedValidation = validationParameters.Clone();
    clonedValidation.LifetimeValidator = null;

    Validators.ValidateLifetime(notBefore, expires, securityToken, clonedValidation);

    return true;
}
```

The clone-and-null pattern avoids infinite recursion: it clones the parameters, removes its own validator, and calls the built-in `Validators.ValidateLifetime` for standard expiry/not-before checks.

The `TokenSecurityMiddleware` wraps the request pipeline to catch `TokenException` thrown during refresh or validation. When a token-related exception occurs, it revokes the offending token ID and returns a 401 with a ProblemDetails response:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);
    }
    catch (TokenException e)
    {
        if (Guid.TryParse(e.TokenId, out var tokenId))
        {
            var expiration = _clock.UtcNow.AddMinutes(
                _settings.AllowedClockSkewMinutes + _settings.RefreshExpirationMinutes);
            ServerLifetimeCache.Revoke(tokenId, expiration);
        }

        var problem = new ProblemDetails
        {
            Detail = "JWT was invalid and has been revoked",
            Status = (int)HttpStatusCode.Unauthorized,
        };

        await Results.Problem(problem).ExecuteAsync(context);
    }
}
```

This ensures that any token that fails validation during a refresh attempt is proactively revoked, preventing replay.

---

## Key Files

- `madera-apps:Madera/Madera.UI.Server/Identity/JwtScheme/DefaultTokenService.cs` — 277-line JWT creation and refresh validation with browser fingerprinting
- `madera-apps:Madera/Madera.UI.Server/BackgroundServices/ServerLifetimeCache.cs` — In-memory revocation cache with lazy TTL pruning and startup pepper
- `madera-apps:Madera/Madera.UI.Server/Identity/JwtScheme/ConfigureJwtBearerOptions.cs` — JWT validation with revocation-aware lifetime validator
- `madera-apps:Madera/Madera.UI.Server/Middlewares/TokenSecurityMiddleware.cs` — Pipeline-level exception-to-revocation handler
- `madera-apps:Madera/Madera.UI.Server/Identity/JwtScheme/JwtSettings.cs` — JWT configuration with pepper-salted signing key
