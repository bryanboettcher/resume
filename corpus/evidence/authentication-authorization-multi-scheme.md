---
title: Authentication and Authorization — Multi-Scheme Setup and Default Policy
tags: [authentication, authorization, jwt, api-key, aspnet-core, security, policy-scheme, forwarding, claim-gating]
related:
  - evidence/authentication-authorization.md
  - evidence/authentication-authorization-jwt-tokens.md
  - evidence/authentication-authorization-api-key.md
  - evidence/authentication-authorization-roles-enforcement.md
  - evidence/aspnet-minimal-api-patterns.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/authentication-authorization.md
---

# Authentication and Authorization — Multi-Scheme Setup and Default Policy

The madera-apps codebase implements a multi-scheme authentication system that routes requests to either JWT bearer or API key validation at request time, without any endpoint-level branching. The default authorization policy adds an extra layer that prevents refresh tokens from being repurposed as access tokens.

---

## Evidence: Multi-Scheme Authentication with Policy-Based Forwarding

The `AuthenticationRegistry.Configure` method in `Madera/Madera.UI.Server/Registries/AuthenticationRegistry.cs` wires up three authentication schemes and a forwarding policy that selects between them at request time:

```csharp
services.AddAuthentication(conf =>
        {
            conf.DefaultScheme = "ForwardingScheme";
            conf.DefaultChallengeScheme = "ForwardingScheme";
        })
        .AddJwtBearer()
        .AddApiKey()
        .AddPolicyScheme(
            "ForwardingScheme",
            "Handles trying multiple authentication types in order",
            options =>
            {
                options.ForwardDefaultSelector = ctx =>
                {
                    var requestServices = ctx.RequestServices;
                    var apiKeyOptions = requestServices
                        .GetRequiredService<IOptions<ApiKeyAuthenticationOptions>>().Value;

                    var headers = ctx.Request.Headers;

                    if (headers.ContainsKey(apiKeyOptions.HeaderName))
                        return ApiKeyAuthenticationOptions.AuthenticationScheme;

                    return JwtBearerDefaults.AuthenticationScheme;
                };
            }
        );
```

The forwarding logic checks whether the incoming request has the API key header (configurable, defaults to `X-Api-Key`). If present, the request routes to the custom `ApiKeyAuthenticationScheme`; otherwise, it falls through to standard JWT bearer validation. This means the same endpoints serve both human users (browser-based JWT) and machine clients (API key) without any endpoint-level branching.

The `.AddApiKey()` call is a custom extension method in `Madera/Madera.UI.Server/Extensions/AuthenticationBuilderExtensions.cs` that registers the options, the handler, and the scheme in one call:

```csharp
public static AuthenticationBuilder AddApiKey(this AuthenticationBuilder builder)
{
    builder.Services.AddOptions<ApiKeyAuthenticationOptions>()
           .BindConfiguration(ApiKeyAuthenticationOptions.AuthenticationScheme);

    builder.Services.AddTransient<ApiKeyAuthenticationScheme>();

    builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationScheme>(
        ApiKeyAuthenticationOptions.AuthenticationScheme,
        "Allows for authenticating users with API keys from a custom header",
        conf => { }
    );

    return builder;
}
```

---

## Evidence: Default Authorization Policy with Claim Gating

The authorization configuration adds a non-obvious security constraint. The default policy requires not just an authenticated user, but specifically the presence of a `c_hash` (browser fingerprint) claim:

```csharp
services.AddAuthorization(conf =>
{
    conf.DefaultPolicy = new AuthorizationPolicyBuilder()
                         .RequireAuthenticatedUser()
                         .RequireClaim(JwtRegisteredClaimNames.CHash)
                         .Build();
});
```

This matters because refresh tokens are intentionally issued without the `c_hash` claim. A refresh token that gets intercepted cannot be used directly as an access token — the authorization policy will reject it at the claim level, even though it is a valid, signed JWT from the same issuer. This is a defense-in-depth measure that prevents refresh tokens from being repurposed.

---

## Key Files

- `madera-apps:Madera/Madera.UI.Server/Registries/AuthenticationRegistry.cs` — Multi-scheme registration with policy forwarding
- `madera-apps:Madera/Madera.UI.Server/Extensions/AuthenticationBuilderExtensions.cs` — Custom `.AddApiKey()` extension
