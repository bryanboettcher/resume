---
title: ASP.NET Minimal API Patterns — Validation, Pagination, Auth Enforcement, and OpenAPI
tags: [aspnet-core, minimal-apis, fluentvalidation, pagination, authorization, openapi, reflection, csharp, kb-platform, madera-apps]
related:
  - evidence/aspnet-minimal-api-patterns.md
  - evidence/aspnet-minimal-api-patterns-static-endpoints.md
  - evidence/aspnet-minimal-api-patterns-reflection-discovery.md
  - projects/kbstore-ecommerce.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/aspnet-minimal-api-patterns.md
---

# ASP.NET Minimal API Patterns — Validation, Pagination, Auth Enforcement, and OpenAPI

Cross-cutting technical patterns across both codebases: contrasting validation strategies (hand-rolled `IsValid()` vs FluentValidation with co-located validators), custom `BindAsync` pagination with page size allowlist, reflection-based convention enforcement tests that fail the build on violations, and OpenAPI integration via tags and `.WithOpenApi()`.

---

## Evidence: Validation Strategy Differences

The two codebases differ in validation approach. kb-platform uses hand-rolled `IsValid()` methods on payload classes:

```csharp
public class CreateSellableItemPayload
{
    public string? Sku { get; set; }
    public string? Name { get; set; }
    public decimal BasePrice { get; set; }
    public string? ItemType { get; set; }

    public bool IsValid()
        => !string.IsNullOrWhiteSpace(Sku)
        && !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(ItemType)
        && BasePrice >= 0;
}
```

madera-apps uses FluentValidation with co-located validators. The `CreateLeadPayloadValidator` shows the level of detail — it validates field lengths, numeric ranges, and uses custom validators (`ValidProcessDateOnlyRangeValidator`, `ValidDateOfBirthValidator`, `ScriptValidator`) for domain-specific rules:

```csharp
// madera-apps: Madera/Madera.UI.Server/Endpoints/DirectMail/CreateLeadEndpoint.cs

public sealed class CreateLeadPayloadValidator : AbstractValidator<CreateLeadPayload>
{
    public CreateLeadPayloadValidator()
    {
        RuleFor(x => x.VerticalId).GreaterThan(0).LessThan(byte.MaxValue);
        RuleFor(x => x.PublisherId).GreaterThan(0).LessThan(short.MaxValue);
        RuleFor(x => x.FirstName).NotEmpty().Length(1, 50);
        RuleFor(x => x.LastName).NotEmpty().Length(1, 50);
        RuleFor(x => x.Address1).NotEmpty().Length(1, 255);
        RuleFor(x => x.City).NotEmpty().Length(1, 255);
        RuleFor(x => x.State).NotEmpty().Length(1, 50);
        RuleFor(x => x.Zip).Length(5);
        RuleFor(x => x.LeadDate).SetValidator(new ValidProcessDateOnlyRangeValidator());
        RuleFor(x => x.DOB).SetValidator(new ValidDateOfBirthValidator());
    }
}
```

---

## Evidence: Pagination

kb-platform uses `[AsParameters]` binding with typed query objects. `InventoryEndpoints.GetAll` binds a `PaginatedQuery` and returns `PaginatedResponse<InventoryModel>`:

```csharp
public static async Task<IResult> GetAll(
    [AsParameters] PaginatedQuery pagination,
    [FromServices] IInventoryQueryService queryService,
    CancellationToken cancellationToken = default)
{
    var result = await queryService.SearchAsync(pagination, cancellationToken).ConfigureAwait(false);
    return Results.Ok(result);
}
```

madera-apps implements a custom `PaginationRequest` with `BindAsync` for parameter binding from query strings. It validates page sizes against an allowlist (`10, 25, 50, 100`) and defaults gracefully:

```csharp
// madera-apps: Madera/Madera.UI.Server/Endpoints/PaginationRequest.cs

public static ValueTask<PaginationRequest?> BindAsync(HttpContext context, ParameterInfo _)
{
    if (!int.TryParse(context.Request.Query[PageNumberParam], CultureInfo.InvariantCulture, out var pageNumber))
        pageNumber = 0;

    if (!int.TryParse(context.Request.Query[PageSizeParam], CultureInfo.InvariantCulture, out var pageSize))
        pageSize = 25;

    if (pageNumber < 0)
        throw new ArgumentOutOfRangeException(PageNumberParam, "pageNumber cannot be negative");

    if (!ValidPageSizes.Contains(pageSize))
        throw new ArgumentOutOfRangeException(PageSizeParam, "pageSize must be one of " + GetAllowableValues());

    return ValueTask.FromResult(new PaginationRequest { PageNumber = pageNumber, PageSize = pageSize })!;
}
```

---

## Evidence: Authorization and Endpoint Standards Enforcement

All madera-apps endpoints carry `[Authorize]` attributes. This is not just convention — it is enforced by a reflection-based test (`EndpointAuthTests.All_endpoints_follow_standards`) that discovers every `IEndpoint` implementation and asserts:

1. Exactly one public static method per endpoint class
2. `[Authorize]` attribute present on the handler
3. Every parameter has a binding source attribute (`[FromBody]`, `[FromServices]`, `[FromRoute]`, `[FromQuery]`, etc.)
4. `PaginationRequest`, if present, must be the first parameter
5. `CancellationToken` must be the last parameter

This test uses Shouldly assertions and scans the entire assembly, meaning any new endpoint that violates these conventions fails the build.

---

## Evidence: OpenAPI Integration

Both codebases integrate with OpenAPI. kb-platform uses `[ProducesResponseType]` attributes on handler methods and `.WithOpenApi()` / `.WithTags()` / `.WithSummary()` on route builders. madera-apps uses `[Tags(EndpointTags.DirectMailCore)]` attributes on handlers and the discovery infrastructure adds `.WithOpenApi()` globally. The `EndpointTags` class defines Swagger groupings by domain area: `DirectMailCore`, `DirectMailImports`, `DirectMailGroupings`, `DirectMailFiles`, `Convoso`, `Dispos`.

---

## Key Files

- `madera-apps:Madera/Madera.UI.Server/Endpoints/PaginationRequest.cs` — Custom BindAsync pagination with page size allowlist
- `madera-apps:Madera/Madera.UI.Server/Endpoints/EndpointTags.cs` — Swagger tag constants by domain area
- `madera-apps:Madera/Madera.UI.Server/Endpoints/DirectMail/Verticals/UpdateVerticalEndpoint.cs` — Update with conditional validation rules
- `madera-apps:Madera/Madera.UI.Server.Tests/EndpointAuthTests.cs` — Reflection-based convention enforcement for all endpoints
