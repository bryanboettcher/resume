---
title: ASP.NET Minimal API Patterns — One-Class-Per-Endpoint with Reflection Discovery (madera-apps)
tags: [aspnet-core, minimal-apis, reflection, endpoint-discovery, masstransit, fluentvalidation, csharp, direct-mail, madera-apps]
related:
  - evidence/aspnet-minimal-api-patterns.md
  - evidence/aspnet-minimal-api-patterns-static-endpoints.md
  - evidence/aspnet-minimal-api-patterns-technical-details.md
  - evidence/masstransit-contract-design.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/aspnet-minimal-api-patterns.md
---

# ASP.NET Minimal API Patterns — One-Class-Per-Endpoint with Reflection Discovery (madera-apps)

madera-apps uses a fundamentally different endpoint organization than kb-platform. Each endpoint is a single sealed class implementing a marker interface. A reflection-based discovery method auto-registers every endpoint at startup, enforcing structural constraints (one public static method, `[Authorize]`, parameter binding attributes, `CancellationToken` last).

---

## Evidence: madera-apps One-Class-Per-Endpoint with Reflection Discovery

Each endpoint is a single sealed class implementing one of three marker interfaces: `IRestEndpoint`, `IRpcEndpoint`, or `IReportEndpoint`. The interface determines the route prefix (`api/`, `rpc/`, or `reports/`):

```csharp
// madera-apps: Madera/Madera.Common/Endpoints/IEndpoint.cs

public interface IEndpoint { }
public interface IRestEndpoint : IEndpoint { }
public interface IRpcEndpoint : IEndpoint { }
public interface IReportEndpoint : IEndpoint { }
```

A typical endpoint file contains three things: the endpoint class, the payload DTO, and the FluentValidation validator. All in one file, all sealed:

```csharp
// madera-apps: Madera/Madera.UI.Server/Endpoints/DirectMail/Brokers/CreateBrokerEndpoint.cs

public sealed class CreateBrokerEndpoint : IRestEndpoint
{
    [Tags(EndpointTags.DirectMailCore)]
    [HttpPost("direct-mail/brokers")]
    [Authorize]
    public static async Task<IResult> Handler(
        [FromBody] CreateBrokerPayload payload,
        [FromServices] IRequestClient<CreateBrokerCommand> client,
        CancellationToken token
    )
    {
        Response response = await client.GetResponse<CreateBrokerResponse, BrokerRequestFailure>(
            payload, token
        );

        return response switch
        {
            (_, CreateBrokerResponse success) => Results.Ok(success),
            (_, BrokerRequestFailure failure) => failure.AsResult(),
            _ => throw new InvalidOperationException("RequestClient did not return success or failure")
        };
    }
}

public sealed class CreateBrokerPayload
{
    public string? Name { get; set; }
}

public sealed class CreateBrokerPayloadValidator : AbstractValidator<CreateBrokerPayload>
{
    public CreateBrokerPayloadValidator()
    {
        RuleFor(p => p.Name).MaximumLength(250);
    }
}
```

### Auto-Registration via Reflection

The key infrastructure is `EndpointSetupExtensions.MapDiscoveredEndpoints`, which uses reflection to auto-register every endpoint at startup. It resolves all `IEndpoint` implementations from DI, uses `Delegate.CreateDelegate` to turn each class's single public static method into a route handler, reads the `HttpMethodAttribute` for verb and route template, and maps the endpoint. It also enforces structural constraints: each endpoint class must have exactly one public static method, and that method cannot be generic.

```csharp
// madera-apps: Madera/Madera.Common/EndpointSetupExtensions.cs

public static void MapDiscoveredEndpoints(this WebApplication app, Type[] additionalFilters)
{
    var endpointRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var endpoints = app.Services.GetServices<IEndpoint>();

    foreach (var endpoint in endpoints)
    {
        var endpointType = endpoint.GetType();
        var endpointHandler = CreateHandlerDelegate(endpointType);
        var httpMethodAttribute = endpointHandler.GetMethodAttribute<HttpMethodAttribute>();

        var route = EndpointHelper.CombineRoute(
            GetRoutePrefix(endpoint),   // "api", "rpc", or "reports"
            httpMethodAttribute.Template
        );

        var routeKey = $"{httpMethodAttribute.HttpMethods.First()}: {route}";
        if (!endpointRoutes.Add(routeKey))
            throw new InvalidOperationException($"Duplicate route found: {routeKey}");

        app.MapMethods(route, httpMethodAttribute.HttpMethods, endpointHandler)
           .AddEndpointFiltersFromSetup(additionalFilters, app.Services)
           .AddEndpointFiltersFromType(endpoint)
           .WithDisplayName(endpointType.Name)
           .DisableAntiforgery()
           .WithOpenApi();
    }
}
```

The discovery also supports per-endpoint filter injection via the `IFiltered` interface, allowing individual endpoints to add `IEndpointFilter[]` beyond the global filter set.

### MassTransit as the HTTP-to-Backend Bridge

The madera-apps endpoints are thin HTTP-to-message-bus translators. The pattern is consistent across 20+ endpoint files: inject `IRequestClient<TCommand>`, call `GetResponse<TSuccess, TFailure>`, and pattern-match the response. The `AsResult()` extension method maps MassTransit failure types to HTTP status codes:

```csharp
// madera-apps: Madera/Madera.UI.Server/Extensions/MessageExtensions.cs

public static IResult AsResult(this FailureEventBase failure)
{
    return failure.FailureType switch
    {
        FailureTypes.Unknown => Results.Problem(failure.ToProblem()),
        FailureTypes.Conflict => Results.Conflict(failure),
        FailureTypes.Missing => Results.NotFound(failure),
        FailureTypes.Invalid => Results.BadRequest(failure),
        _ => throw new ArgumentOutOfRangeException(nameof(failure))
    };
}
```

This means the HTTP layer never contains business logic. Domain rules, state transitions, and persistence all happen behind the message bus. The endpoint's only job is payload validation, message dispatch, and response mapping.

Some endpoints use `IPublishEndpoint` for fire-and-forget operations. The `ConvosoBulkDownloadEndpoint` publishes one `SubmitJob<DownloadConvosoFtpMessage>` per date in a range, generating deterministic `JobId` values via `NewId.NextGuid()`:

```csharp
// madera-apps: Madera/Madera.UI.Server/Endpoints/Convoso/Management/BulkDownloadEndpoint.cs

while (startDate <= endDate)
{
    var jobId = NewId.NextGuid();
    var targetDate = DateOnly.FromDayNumber(startDate++);
    jobs.Add(targetDate, jobId);

    await endpoint.Publish<SubmitJob<DownloadConvosoFtpMessage>>(new
    {
        JobId = jobId,
        Job = new { TargetDate = targetDate, CheckCount = 0 }
    }, token);
}
return Results.Ok(jobs);
```

---

## Key Files

- `madera-apps:Madera/Madera.Common/Endpoints/IEndpoint.cs` — Marker interface hierarchy (IRestEndpoint, IRpcEndpoint, IReportEndpoint)
- `madera-apps:Madera/Madera.Common/EndpointSetupExtensions.cs` — Reflection-based endpoint discovery and route registration
- `madera-apps:Madera/Madera.Common/Endpoints/IFiltered.cs` — Per-endpoint filter injection interface
- `madera-apps:Madera/Madera.UI.Server/Extensions/MessageExtensions.cs` — MassTransit failure-to-IResult mapping
- `madera-apps:Madera/Madera.UI.Server/Endpoints/DirectMail/Brokers/CreateBrokerEndpoint.cs` — Canonical request/response pattern
- `madera-apps:Madera/Madera.UI.Server/Endpoints/DirectMail/CreateLeadEndpoint.cs` — Complex payload with address normalization
- `madera-apps:Madera/Madera.UI.Server/Endpoints/Convoso/Management/BulkDownloadEndpoint.cs` — Fire-and-forget job submission via IPublishEndpoint
- `madera-apps:Madera/Madera.UI.Server/Endpoints/DirectMail/Imports/CreateImportEndpoint.cs` — Import creation bridging to saga
