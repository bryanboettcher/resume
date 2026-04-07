---
title: Vertical Slice CQRS — Cross-Domain Event Propagation
tags: [cqrs, masstransit, event-driven, domain-events, idempotency, bounded-contexts, csharp]
related:
  - evidence/vertical-slice-cqrs-validation.md
  - evidence/vertical-slice-cqrs-validation-structure.md
  - evidence/vertical-slice-cqrs-validation-message-contracts.md
  - evidence/vertical-slice-cqrs-validation-test-harness.md
  - evidence/distributed-systems-architecture.md
  - projects/kbstore-ecommerce.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/vertical-slice-cqrs-validation.md
---

# Vertical Slice CQRS — Cross-Domain Event Propagation

In the KbStore e-commerce platform (kb-platform), the API gateway service hosts consumers that bridge bounded contexts. When the Catalog domain publishes a domain event (e.g., `ProductCreated`), a consumer in the API gateway translates it into a Storefront command and handles idempotency explicitly — duplicate event delivery is treated as expected behavior rather than an error condition.

---

## Evidence: Cross-Domain Event Propagation

The API gateway service hosts consumers that bridge bounded contexts. When the Catalog domain publishes a `ProductCreated` event, a consumer in the API gateway creates the corresponding Storefront `SellableItem`:

```csharp
// src/services/KbStore.ApiService/Consumers/Catalog/ProductCreatedConsumer.cs
public async Task Consume(ConsumeContext<ProductCreated> context)
{
    var msg = context.Message;

    var response = await _requestClient.GetResponse<CreateSellableItemResponse>(
        new
        {
            ProductId = (Guid?)msg.ProductId,
            Sku = msg.Sku,
            Name = msg.Name ?? msg.Sku,
            BasePrice = 0m,
            ItemType = "Product",
            Payload = payload,
            Timestamp = DateTimeOffset.UtcNow
        },
        context.CancellationToken);

    // Verify deterministic correlation was maintained
    if (response.Message.Id != msg.ProductId)
        throw new InvalidOperationException("Deterministic correlation failed");
}
```

The consumer handles idempotency explicitly: if the SellableItem already exists for that SKU (duplicate event delivery), the exception is caught and logged as expected behavior rather than retried. It also handles the case where the target entity is in a state that prevents the operation (discontinued item).

Ten consumers in `src/services/KbStore.ApiService/Consumers/Catalog/` handle the full lifecycle: `ProductCreatedConsumer`, `ProductNameUpdatedConsumer`, `ProductDimensionsUpdatedConsumer`, `ProductDiscontinuedConsumer`, `ProductDeletedConsumer`, and corresponding inventory event consumers. Each follows the same pattern: receive Catalog event, translate to Storefront command, handle fault scenarios.

## Key Files

- `kb-platform:src/services/KbStore.ApiService/Consumers/Catalog/ProductCreatedConsumer.cs` — Cross-domain event consumer
- `kb-platform:src/services/KbStore.ApiService/Consumers/Catalog/ProductNameUpdatedConsumer.cs` — Cross-domain update propagation
