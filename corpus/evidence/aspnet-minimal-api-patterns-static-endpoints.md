---
title: ASP.NET Minimal API Patterns — Static Endpoint Classes with MapTo (kb-platform)
tags: [aspnet-core, minimal-apis, rest, cqrs, endpoint-routing, vertical-slice, csharp, kb-platform]
related:
  - evidence/aspnet-minimal-api-patterns.md
  - evidence/aspnet-minimal-api-patterns-reflection-discovery.md
  - evidence/aspnet-minimal-api-patterns-technical-details.md
  - evidence/vertical-slice-cqrs-validation.md
  - projects/kbstore-ecommerce.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/aspnet-minimal-api-patterns.md
---

# ASP.NET Minimal API Patterns — Static Endpoint Classes with MapTo (kb-platform)

In kb-platform, each bounded context gets one static class containing all handler methods and a `MapTo` method that registers routes. This approach groups all endpoint handlers for a domain area into a single, navigable file.

---

## Evidence: kb-platform Static Endpoint Classes with `MapTo`

`SellableItemEndpoints` is the largest endpoint class at 435 lines, covering 17 endpoints across CRUD, state transitions (publish/hide/discontinue/reinstate), and tag management:

```csharp
// kb-platform: src/services/KbStore.ApiService/Endpoints/Storefront/SellableItemEndpoints.cs

public static class SellableItemEndpoints
{
    public static async Task<IResult> Create(
        [FromBody] CreateSellableItemPayload payload,
        [FromServices] ISellableItemCommandService commandService,
        CancellationToken cancellationToken)
    {
        if (!payload.IsValid())
            return Results.BadRequest();

        var typedPayload = PayloadConverter.ConvertDictionaryToPayload(payload.Payload);
        if (typedPayload == null)
            return Results.BadRequest("Invalid or missing payload");

        var result = await commandService.CreateAsync(
            payload.Sku!, payload.Name!, payload.Description,
            payload.BasePrice, payload.ItemType!, typedPayload,
            payload.ProductId, cancellationToken
        ).ConfigureAwait(false);

        return Results.Ok(result);
    }

    // ... 16 more handlers ...

    public static void MapTo(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/sellableitems");

        group.MapPost("", Create);
        group.MapGet("{id:guid}", GetById);
        group.MapGet("by-sku", GetBySku);
        group.MapGet("", GetAll);

        group.MapPatch("{id:guid}/name", UpdateName);
        group.MapPatch("{id:guid}/description", UpdateDescription);
        group.MapPatch("{id:guid}/price", UpdatePrice);
        group.MapPatch("{id:guid}/payload", UpdatePayload);

        group.MapPatch("{id:guid}/publish", Publish);
        group.MapPatch("{id:guid}/hide", Hide);
        group.MapPatch("{id:guid}/discontinue", Discontinue);
        group.MapPatch("{id:guid}/reinstate", Reinstate);

        group.MapDelete("{id:guid}", Delete);

        group.MapGet("{id:guid}/tags", GetItemTags);
        group.MapPost("{id:guid}/tags", AddTagToItem);
        group.MapDelete("{id:guid}/tags/{tagId:guid}", RemoveTagFromItem);
        group.MapDelete("{id:guid}/tags", RemoveAllTags);
    }
}
```

Route composition is hierarchical. `WebApplicationExtensions.MapApplicationEndpoints` creates nested groups so that the full route to a sellable item is `/api/storefront/sellableitems/{id}`:

```csharp
// kb-platform: src/services/KbStore.ApiService/Endpoints/WebApplicationExtensions.cs

public static void MapApplicationEndpoints(this WebApplication app, IEndpointRouteBuilder apiGroup)
{
    var catalogGroup = apiGroup.MapGroup("/catalog");
    InventoryEndpoints.MapTo(catalogGroup);
    ProductEndpoints.MapTo(catalogGroup);

    var storefrontGroup = apiGroup.MapGroup("/storefront");
    SellableItemEndpoints.MapTo(storefrontGroup);

    Public.ProductEndpoints.MapTo(app);
}
```

The CQRS split is visible at the endpoint level: command handlers take `ISellableItemCommandService` while list queries take `ISellableItemQueryService`. State transitions use PATCH on sub-resources (`/publish`, `/hide`, `/discontinue`, `/reinstate`) rather than generic PUT with a status field, encoding the valid transitions into the route structure itself.

kb-platform endpoints call command and query services directly. The message bus boundary exists, but it is behind the service abstraction — the endpoint calls `ISellableItemCommandService.CreateAsync()`, and that service internally uses MassTransit request/response. The endpoint itself does not interact with `IRequestClient` or `IPublishEndpoint`.

---

## Key Files

- `kb-platform:src/services/KbStore.ApiService/Endpoints/Storefront/SellableItemEndpoints.cs` — 435-line endpoint class with 17 handlers and MapTo registration
- `kb-platform:src/services/KbStore.ApiService/Endpoints/Catalog/ProductEndpoints.cs` — Product CRUD with PATCH-per-field pattern
- `kb-platform:src/services/KbStore.ApiService/Endpoints/Catalog/InventoryEndpoints.cs` — Inventory operations with paginated search
- `kb-platform:src/services/KbStore.ApiService/Endpoints/WebApplicationExtensions.cs` — Hierarchical route group composition
- `kb-platform:src/services/KbStore.ApiService/Endpoints/ResponseExtensions.cs` — Failure-to-HTTP-status mapping
- `kb-platform:src/services/KbStore.ApiService/Endpoints/Public/ProductEndpoints.cs` — Read-only public endpoints with separate route group
