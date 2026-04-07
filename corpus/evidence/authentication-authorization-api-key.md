---
title: Authentication and Authorization — API Key Handler and BCrypt Password Hashing
tags: [authentication, api-key, aspnet-core, authenticationhandler, bcrypt, password-hashing, security, service-account]
related:
  - evidence/authentication-authorization.md
  - evidence/authentication-authorization-multi-scheme.md
  - evidence/authentication-authorization-jwt-tokens.md
  - evidence/authentication-authorization-roles-enforcement.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/authentication-authorization.md
---

# Authentication and Authorization — API Key Handler and BCrypt Password Hashing

This document covers the API key authentication handler for machine-to-machine access and the BCrypt password hasher adapter for the existing password store.

---

## Evidence: API Key Authentication Handler

The `ApiKeyAuthenticationScheme` (`Madera/Madera.UI.Server/Identity/ApiKeyScheme/ApiKeyAuthenticationScheme.cs`) is a full `AuthenticationHandler<T>` implementation for service account access. It validates the API key as a parseable GUID, looks up the corresponding service account in the database via `IServiceAccountService`, checks expiration, and builds a `ClaimsPrincipal` with the same claim structure as JWT tokens (sub, exp, iat, nbf, c_hash, profile claims).

The handler follows a deliberate three-tier response pattern:
- **No header present**: Returns `AuthenticateResult.NoResult()`, allowing the forwarding scheme to fall through to JWT
- **Header present but not a valid GUID**: Throws `BadRequestException` (400), stopping the pipeline
- **Valid GUID but no matching/expired account**: Returns `AuthenticateResult.Fail()` (401), with intentionally vague error messages while logging detailed reasons server-side to avoid leaking account state

```csharp
if (account is null)
    Logger.LogWarning("{key} was not a valid account, request ended early", headerValue);
else
    Logger.LogWarning("{key} was a valid account, but it's now expired", headerValue);

return AuthenticateResult.Fail($"{headerValue} is not authorized to make this request");
```

The vague external message with detailed server-side logging prevents adversaries from discovering whether an API key is valid-but-expired vs. never-registered.

---

## Evidence: Legacy Password Hashing

The `LegacyPasswordHasher` (`Madera/Madera.UI.Server/Identity/Services/LegacyPasswordHasher.cs`) implements `IPasswordHasher<MaderaAccount>` using BCrypt (cost factor 10). The name "Legacy" indicates this wraps an existing password store — the platform was migrating from a previous system where passwords were already BCrypt-hashed, and this adapter lets ASP.NET Identity's `IPasswordHasher<T>` interface work with those existing hashes without a migration:

```csharp
public sealed class LegacyPasswordHasher : IPasswordHasher<MaderaAccount>
{
    public string HashPassword(MaderaAccount user, string password)
        => BCrypt.Net.BCrypt.HashPassword(password, 10);

    public PasswordVerificationResult VerifyHashedPassword(
        MaderaAccount user, string hashedPassword, string providedPassword)
        => BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword)
            ? PasswordVerificationResult.Success
            : PasswordVerificationResult.Failed;
}
```

---

## Key Files

- `madera-apps:Madera/Madera.UI.Server/Identity/ApiKeyScheme/ApiKeyAuthenticationScheme.cs` — Full AuthenticationHandler with three-tier response pattern
- `madera-apps:Madera/Madera.UI.Server/Identity/ApiKeyScheme/ApiKeyAuthenticationOptions.cs` — Configurable header name
- `madera-apps:Madera/Madera.UI.Server/Identity/Services/LegacyPasswordHasher.cs` — BCrypt adapter for IPasswordHasher<T>
