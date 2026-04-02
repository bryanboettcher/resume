---
title: Vertical Slice CQRS — Typed Exception Hierarchy with Fault Reconstitution
tags: [cqrs, domain-exceptions, masstransit, fault-handling, ddd, csharp, request-response]
related:
  - evidence/vertical-slice-cqrs-validation.md
  - evidence/vertical-slice-cqrs-validation-structure.md
  - evidence/vertical-slice-cqrs-validation-layered-validation.md
  - evidence/vertical-slice-cqrs-validation-message-contracts.md
  - evidence/distributed-systems-architecture.md
  - projects/kbstore-ecommerce.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/vertical-slice-cqrs-validation.md
---

# Vertical Slice CQRS — Typed Exception Hierarchy with Fault Reconstitution

The KbStore e-commerce platform (kb-platform) implements per-bounded-context exception hierarchies with MassTransit fault reconstitution. When a state machine throws a domain exception across the message boundary, extension methods on the command service side reconstruct the original typed exception — making the message bus transparent to callers. Each bounded context (Catalog, Storefront) has its own parallel hierarchy and reconstitution logic.

---

## Evidence: Typed Exception Hierarchy with Fault Reconstitution

Each bounded context defines a domain-specific exception hierarchy rooted in a base exception class. The Product domain, for example:

```
ProductException (abstract base)
├── GenericProductException — unmapped/unknown faults
├── ProductNotFoundException — entity lookup failures
├── ProductValidationException — business rule violations
├── ProductConflictException — duplicate SKU, linked inventory conflicts
└── ProductStateException — invalid state transitions
```

`ProductConflictException` and `ProductValidationException` provide static factory methods for common scenarios (e.g., `ProductConflictException.DuplicateSku(sku)`, `ProductValidationException.InvalidStockThreshold(threshold)`), attaching structured `Data` dictionary entries for diagnostic context.

**Source:** `src/services/KbStore.Catalog.Abstractions/Exceptions/Products.cs` (189 lines)

### Fault Reconstitution Across the Message Boundary

When a state machine throws a domain exception, MassTransit serializes it as a `RequestFaultException`. The command service layer uses extension methods to reconstitute the original typed exception from the fault:

```csharp
// src/services/KbStore.Catalog.Services/Extensions/ProductRequestFaultExceptionExtensions.cs
public static ProductException ToProductException(this RequestFaultException faultException)
{
    var fault = faultException.Fault?.Exceptions.FirstOrDefault();
    var typeName = fault.ExceptionType?.Split('.').LastOrDefault() ?? "";
    return typeName switch
    {
        nameof(ProductNotFoundException) => RecreateNotFoundException(message, data),
        nameof(ProductConflictException) => RecreateConflictException(message, data),
        nameof(ProductStateException) => RecreateStateException(message, data),
        nameof(ProductValidationException) => new ProductValidationException(message),
        _ => new GenericProductException(message)
    };
}
```

This means callers of `IProductCommandService` catch typed domain exceptions (`ProductNotFoundException`, `ProductStateException`) regardless of whether the exception originated locally (validation) or remotely (state machine fault). The message boundary is transparent to the caller. Each bounded context (Catalog, Storefront) has its own parallel exception hierarchy and fault reconstitution extensions.

## Key Files

- `kb-platform:src/services/KbStore.Catalog.Abstractions/Exceptions/Products.cs` — Typed exception hierarchy
- `kb-platform:src/services/KbStore.Catalog.Services/Extensions/ProductRequestFaultExceptionExtensions.cs` — Fault reconstitution
- `kb-platform:src/infrastructure/KbStore.Abstractions/Contracts.cs` — Shared failure contract base
