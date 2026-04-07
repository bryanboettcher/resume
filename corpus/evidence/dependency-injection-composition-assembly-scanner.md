---
title: DI Composition — Custom Assembly Scanner
tags: [dependency-injection, assembly-scanning, convention-over-configuration, csharp, aspnet-core, service-registration, attributes, direct-mail]
related:
  - evidence/dependency-injection-composition.md
  - evidence/dependency-injection-composition-registry-pattern.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/dependency-injection-composition.md
---

# DI Composition — Custom Assembly Scanner

Rather than relying on third-party scanning libraries or manual per-service registration, Madera uses a custom `ScanAssembly` extension method that performs convention-based registration with namespace exclusions and attribute escape hatches.

---

## Evidence: Custom Assembly Scanner

The `ScanAssembly` extension method (`Madera/Madera.Common/IoC/ServiceCollectionExtensions.cs`, 97 lines) performs convention-based registration:

```csharp
public static void ScanAssembly(this IServiceCollection services, Assembly assembly)
{
    var candidateTypes = assembly.GetTypes()
        .Where(t => t is { IsPublic: true, IsClass: true, IsAbstract: false });

    foreach (var type in candidateTypes)
    {
        if (type.GetCustomAttribute<ExcludeFromScannerAttribute>() is not null)
            continue;

        // ... namespace filtering, interface enumeration, registration
    }
}
```

The scanner excludes types from framework namespaces (`Dapper`, `Microsoft`, `System`, `MassTransit`, `Madera.Contracts`) and specific interface types (`IConfigureOptions<>`, `IEquatable<>`, `IDisposable`, `IAsyncDisposable`). It supports two custom attributes:

- `[Lifetime(ServiceLifetime.Scoped)]` — overrides the default transient lifetime
- `[ExcludeFromScanner]` — opts a class or interface out of automatic registration

This gives the team convention-over-configuration for simple service registrations while keeping explicit control through the attribute escape hatches. The namespace exclusion list prevents the scanner from accidentally registering framework internals or message contracts as services.

The custom scanner fills the gap between "register everything by hand" and "bring in a full IoC container like Lamar." Bryan has experience with both approaches — he contributed a fix to Lamar (PR #362) and chose to build a lightweight scanner for this project instead.

---

## Key Files

- `madera-apps:Madera/Madera.Common/IoC/ServiceCollectionExtensions.cs` — Custom assembly scanner with namespace filtering and attribute overrides
