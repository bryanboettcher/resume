---
title: DI Composition — Generic Pipeline Composition and MassTransit Partial Classes
tags: [dependency-injection, generics, etl-pipeline, masstransit, partial-class, csharp, factory-pattern, config-driven, direct-mail]
related:
  - evidence/dependency-injection-composition.md
  - evidence/dependency-injection-composition-registry-pattern.md
  - evidence/etl-pipeline-framework.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/dependency-injection-composition.md
---

# DI Composition — Generic Pipeline Composition and MassTransit Partial Classes

Two patterns in Madera eliminate per-domain boilerplate at registration time: a type-constrained generic `DataflowsRegistry.Configure<>()` method that wires entire ETL pipeline subsystems from type parameters, and a `partial class` decomposition for MassTransit configuration that separates transport, scheduling, message data, and serialization concerns into distinct files.

---

## Evidence: Generic Pipeline Composition

`DataflowsRegistry` (`Madera/Madera.Dataflows.Common/Registries/DataflowsRegistry.cs`, 111 lines) is a generic registration method that wires up an entire ETL pipeline subsystem from type parameters:

```csharp
public static void Configure<TPipelineData, TOptions, TRuntimeData>(
    IConfiguration context, 
    IServiceCollection services, 
    IDictionary<PipelineSinkProviders, Type>? additionalSinkProviders = null)
    where TPipelineData: class
    where TOptions: class, IPipelineConfiguration, IConfigurationSectionProvider
    where TRuntimeData: class, IRuntimeData
```

The three type parameters constrain the registration: `TPipelineData` is the row type flowing through the pipeline, `TOptions` provides configuration (and must implement both `IPipelineConfiguration` and `IConfigurationSectionProvider` for self-binding), and `TRuntimeData` carries per-request state like the import source stream.

Inside, it uses factory lambdas to resolve pipeline components based on configuration at resolve-time, not registration-time:

```csharp
services.AddScoped(ctx => {
    var runtimeData = ctx.GetRequiredService<TRuntimeData>();
    return runtimeData.FileType switch
    {
        SourceFileType.Csv => ActivatorUtilities.GetServiceOrCreateInstance<CsvStreamPipelineSource<TPipelineData>>(ctx),
        SourceFileType.Tsv => ActivatorUtilities.GetServiceOrCreateInstance<TsvStreamPipelineSource<TPipelineData>>(ctx),
        SourceFileType.Psv => ActivatorUtilities.GetServiceOrCreateInstance<PsvStreamPipelineSource<TPipelineData>>(ctx),
        // ...
    };
});
```

Each domain calls `DataflowsRegistry.Configure<>()` with its own types and additional sink providers. For example, `DirectMailRegistry` passes `DirectMailBulkCopyPipelineSink` as the bulk sink:

```csharp
DataflowsRegistry.Configure<DirectMailData, DirectMailOptions, DirectMailRuntimeData>(
    context, services,
    new Dictionary<PipelineSinkProviders, Type>()
    {
        { PipelineSinkProviders.Bulk, typeof(DirectMailBulkCopyPipelineSink) }
    }
);
```

The `DispoRegistry` goes further, using a local function to register multiple vendor-specific pipeline types through the same generic infrastructure:

```csharp
services.AddScoped<IPipelineProcessor<TranzactDispoData>, TranzactPipelineProcessor>();
Register<TranzactDispoData>();

// Clearview-specific (commented out, ready for activation)
// services.AddScoped<IPipelineProcessor<ClearviewDispoData>, ClearviewPipelineProcessor>();
// Register<ClearviewDispoData>();

void Register<TData>() where TData : BaseDispoData
{
    DataflowsRegistry.Configure<TData, DispoOptions, DispoRuntimeData>(
        context, services,
        new Dictionary<PipelineSinkProviders, Type>()
        {
            { PipelineSinkProviders.Bulk, typeof(DispoBulkCopyPipelineSink<TData>) }
        }
    );
}
```

---

## Evidence: MassTransit Configuration as Partial Class Decomposition

MassTransit configuration in Madera is split across a `partial class` (`Madera/Madera.Common/Startup/MassTransitStartup.cs` and related files) with each concern in its own file:

- `MassTransitStartup.cs` — top-level `ConfigureBackend()` and `ConfigureWebserver()` orchestrators
- `MassTransitStartup_Transport.cs` — bus transport selection (InMemory, RabbitMQ, Azure Service Bus)
- `MassTransitStartup_Scheduling.cs` — Hangfire vs. transport-level message scheduling
- `MassTransitStartup_MessageData.cs` — large message storage (local filesystem vs. Azure Blob)
- `MassTransitStartup_Serialization.cs` — JSON serializer configuration, kebab-case endpoint formatting
- `MassTransitStartup_JobConsumers.cs` — job consumer infrastructure

The backend vs. webserver distinction matters: `ConfigureBackend` includes job consumers and scheduling (needed for background processing), while `ConfigureWebserver` omits them:

```csharp
public static void ConfigureBackend(HostBuilderContext context, IBusRegistrationConfigurator services, string applicationName)
{
    // ...
    ConfigureJobConsumers(context, services);
    ConfigureScheduling(context, services);
    ConfigureMessageData(context, services);
    ConfigureSerialization(context, services);
    ConfigureTransport(context, services);
}

public static void ConfigureWebserver(HostBuilderContext context, IBusRegistrationConfigurator services, string applicationName)
{
    // ...
    ConfigureMessageData(context, services);
    ConfigureSerialization(context, services);
    ConfigureTransport(context, services);
}
```

The transport layer uses a `BusCallbacks` collection — a static `ICollection<Action<IBusRegistrationContext, IBusFactoryConfigurator>>` — that lets domain registries hook into the bus configuration pipeline. `DirectMailRegistry` uses this to register a global consume filter:

```csharp
MassTransitStartup.BusCallbacks.Add(RegisterAddressHashFilter);

private static void RegisterAddressHashFilter(IBusRegistrationContext ctx, IBusFactoryConfigurator cfg)
{
    cfg.UseConsumeFilter(typeof(AddressHashConsumeFilter<>), ctx);
}
```

---

## Key Files

- `madera-apps:Madera/Madera.Dataflows.Common/Registries/DataflowsRegistry.cs` — Generic pipeline registration with type-constrained composition
- `madera-apps:Madera/Madera.Dataflows.DirectMail/DirectMailRegistry.cs` — Largest domain registry using DataflowsRegistry.Configure
- `madera-apps:Madera/Madera.Dataflows.Dispos/DispoRegistry.cs` — Generic local function pattern for per-vendor pipeline registration
- `madera-apps:Madera/Madera.Common/Startup/MassTransitStartup.cs` — Partial class orchestrator for MassTransit subsystem configuration
- `madera-apps:Madera/Madera.Common/Startup/MassTransitStartup_Transport.cs` — Config-driven bus transport selection (InMemory/RabbitMQ/Azure Service Bus)
