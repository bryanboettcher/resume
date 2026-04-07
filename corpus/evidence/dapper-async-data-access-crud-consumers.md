---
title: Dapper Data Access — CRUD Consumers with Dapper.Contrib
tags: [dapper, dapper-contrib, masstransit, csharp, crud, sql-server, table-attributes, direct-mail]
related:
  - evidence/dapper-async-data-access.md
  - evidence/dapper-async-data-access-connection-provider.md
  - evidence/masstransit-contract-design.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/dapper-async-data-access.md
---

# Dapper Data Access — CRUD Consumers with Dapper.Contrib

Reference data entities (Brokers, Publishers, Verticals, Creatives, MailHouses) in the Madera platform are managed through MassTransit message consumers that use `Dapper.Contrib.Extensions` for simple CRUD operations. Each consumer class implements four `IConsumer<T>` interfaces for Create, Update, Delete, and Validate operations.

---

## Evidence: CRUD Consumers with Dapper.Contrib

The pattern is consistent across all five entity types:

```csharp
// VerticalConsumers.cs
public async Task Consume(ConsumeContext<CreateVerticalCommand> context)
{
    await using var connection = _provider.CreateConnection();
    var model = new DbVerticalModel { Name = msg.Name, ... };

    var id = await connection.InsertAsync(model);
    model.Id = id;

    await context.Publish<VerticalCreatedEvent>(model);
    await context.RespondIf<CreateVerticalResponse>(model);
}
```

The private `DbVerticalModel` class uses `Dapper.Contrib` attributes to map to the database:

```csharp
[Table("ref.Verticals")]
private class DbVerticalModel
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? DisplayName { get; set; }
    public double? MinimumAge { get; set; }
    public double? MaximumAge { get; set; }
}
```

The `[Table]` attribute includes the schema prefix (`ref.Verticals`), and the `[Key]` attribute tells Dapper.Contrib to use identity insert and return the generated key. The `PublisherConsumers` also uses the `[Computed]` attribute on `FreeRemail` to exclude a database-computed column from INSERT/UPDATE statements.

Each consumer follows the same error handling pattern: try the operation, publish a domain event on success (`VerticalCreatedEvent`), and respond to the caller with either a success response or a typed failure. The `RespondIf` extension method (from `Madera.Common.Extensions`) handles request/response optionality — if the consumer was invoked via `Publish` rather than `Request`, there's no response address and the response is silently skipped.

---

## Key Files

- `madera-apps:Madera/Madera.Dataflows.DirectMail/Consumers/Core/VerticalConsumers.cs` — CRUD consumer pattern using Dapper.Contrib with [Table] and [Key] attributes
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Consumers/Core/PublisherConsumers.cs` — CRUD consumer with [Computed] attribute for database-computed columns
