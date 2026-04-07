---
title: Vertical Slice CQRS — Layered Validation Without a Framework
tags: [cqrs, validation, aspnet-core, domain-exceptions, ddd, masstransit, request-response]
related:
  - evidence/vertical-slice-cqrs-validation.md
  - evidence/vertical-slice-cqrs-validation-structure.md
  - evidence/vertical-slice-cqrs-validation-exception-hierarchy.md
  - evidence/vertical-slice-cqrs-validation-message-contracts.md
  - evidence/distributed-systems-architecture.md
  - projects/kbstore-ecommerce.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/vertical-slice-cqrs-validation.md
---

# Vertical Slice CQRS — Layered Validation Without a Framework

The KbStore e-commerce platform implements a three-layer validation pipeline without FluentValidation or any third-party validation library. Each layer is responsible for what it can meaningfully check at that point in the pipeline: structural correctness at the HTTP boundary, business rules at the command service layer, and state-transition rules at the state machine. This separation ensures the message bus only ever sees commands that have already cleared both structural and domain validation.

---

## Evidence: Layered Validation Without a Validation Framework

KbStore does not use FluentValidation or any third-party validation library. Instead, validation is distributed across three layers, with each layer responsible for what it can meaningfully check at that point in the pipeline.

### Layer 1: Endpoint Payload Validation

Each endpoint defines a payload class with an `IsValid()` method that checks structural correctness — the kind of validation that should reject a request before it ever reaches the domain:

```csharp
// src/services/KbStore.ApiService/Endpoints/Catalog/ProductEndpoints.cs
public class CreateProductPayload
{
    public string? Sku { get; set; }
    public string? Name { get; set; }
    public ProductDimensions? Dimensions { get; set; }
    public int Quantity { get; set; }
    public Guid? InventoryId { get; set; }
    public int? StockThreshold { get; set; }
    public TimeSpan? LeadTime { get; set; }

    public bool IsValid()
        => !string.IsNullOrWhiteSpace(Sku)
        && (StockThreshold == null || StockThreshold >= 0)
        && (LeadTime == null || LeadTime.Value.TotalSeconds >= 0);
}
```

The endpoint checks `payload.IsValid()` and returns `Results.BadRequest()` immediately if it fails. This is deliberate: basic structural validation (required fields, range checks on optional parameters) belongs at the HTTP boundary, before allocating a message bus request.

The Storefront's `CreateSellableItemPayload` follows the same pattern but adds item-type and pricing checks:

```csharp
// src/services/KbStore.ApiService/Endpoints/Storefront/SellableItemEndpoints.cs
public bool IsValid()
    => !string.IsNullOrWhiteSpace(Sku)
    && !string.IsNullOrWhiteSpace(Name)
    && !string.IsNullOrWhiteSpace(ItemType)
    && BasePrice >= 0;
```

### Layer 2: Command Service Domain Validation

The command service layer (`MassTransitProductCommandService`, `SellableItemCommandService`) performs business-rule validation that requires domain knowledge. These checks throw typed domain exceptions rather than returning error codes:

```csharp
// src/services/KbStore.Catalog.Services/MassTransitProductCommandService.cs
public async Task<ProductModel> CreateAsync(string sku, string? name, ...)
{
    if (string.IsNullOrWhiteSpace(sku))
        throw new ProductValidationException("SKU must have a value");

    if (string.IsNullOrWhiteSpace(name))
        throw new ProductValidationException("Name must have a value");

    if (quantity <= 0)
        throw new ProductValidationException("Quantity must be a positive value");

    if (stockThreshold < 0)
        throw ProductValidationException.InvalidStockThreshold(stockThreshold);

    if (leadTime?.TotalSeconds < 0)
        throw ProductValidationException.InvalidLeadTime(leadTime);

    // Only after validation passes does the command get sent to the bus
    var client = _clientFactory.CreateRequestClient<CreateProductRequest>();
    var response = await client.GetResponse<CreateProductResponse>(new { ... }, cancellationToken)
        .ConfigureAwait(false);
    return response.Message;
}
```

This creates a guard layer: if validation fails, no MassTransit message is ever published. The bus only sees commands that have already passed both structural and domain validation.

### Layer 3: State Machine State Validation

The state machine itself enforces state-transition rules (e.g., "cannot update a discontinued product"). When a command arrives for an entity in an invalid state, the state machine throws a `ProductStateException`. These exceptions propagate back through MassTransit's request/response fault mechanism and are reconstituted at the command service layer.

## Key Files

- `kb-platform:src/services/KbStore.ApiService/Endpoints/Catalog/ProductEndpoints.cs` — HTTP endpoint with payload validation
- `kb-platform:src/services/KbStore.ApiService/Endpoints/Storefront/SellableItemEndpoints.cs` — Storefront endpoint with payload validation
- `kb-platform:src/services/KbStore.Catalog.Services/MassTransitProductCommandService.cs` — Command service with domain validation and MassTransit dispatch
- `kb-platform:src/services/KbStore.Storefront.Services/SellableItemCommandService.cs` — Storefront command service
