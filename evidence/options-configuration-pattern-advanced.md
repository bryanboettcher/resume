---
title: Options Configuration Pattern — Advanced Usage (Sensitive Data, IConfigureOptions, Consumption Patterns, Inheritance)
tags: [options-pattern, configuration, aspnet-core, csharp, sensitive-data, iconfigureoptions, iconfigurenamedoptions, options-inheritance, dependency-injection, direct-mail]
related:
  - evidence/options-configuration-pattern.md
  - evidence/options-configuration-pattern-design-conventions.md
  - evidence/dependency-injection-composition.md
  - evidence/authentication-authorization.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/options-configuration-pattern.md
---

# Options Configuration Pattern — Advanced Usage

This document covers the more sophisticated options patterns in the Madera codebase: the `[SensitiveData]`/`ICloneable` convention for safe configuration dumps, `IConfigureOptions<T>` and `IConfigureNamedOptions<T>` for complex binding, options consumption patterns in consumers and factory lambdas, and options class inheritance.

---

## Evidence: Sensitive Data Protection in Options

The Madera codebase implements a custom `[SensitiveData]` attribute and `ICloneable` pattern across every options class to enable safe configuration dumping. The `Clone()` method returns an anonymous object with sensitive fields run through `Sanitize()`:

```csharp
public sealed class ConvosoOptions : IConnectionStringProvider, IPipelineConfiguration,
    IConfigurationSectionProvider, ICloneable
{
    [SensitiveData]
    public required string ConnectionString { get; init; }
    [SensitiveData]
    public required string DownloadUsername { get; init; }
    [SensitiveData]
    public required string DownloadPassword { get; init; }

    public object Clone()
    {
        return new
        {
            ConnectionString = ConnectionString.Sanitize(SanitizeTypes.ConnectionString),
            DownloadUsername = DownloadUsername.Sanitize(SanitizeTypes.Default),
            DownloadPassword = DownloadPassword.Sanitize(SanitizeTypes.Complete),
            // non-sensitive fields pass through unchanged
            ImportBatchSize,
            ImportBatchTable,
            DownloadEnabled,
        };
    }
}
```

The `SanitizeTypes` enum controls how much of the original value is preserved — `ConnectionString` might show the server name but mask credentials, `ApiKey` might show the first few characters, `Complete` replaces the entire value. This appears consistently across `BusTransportOptions`, `MessageDataOptions`, `MessageSchedulingOptions`, `ExtendedJobOptions`, `LobNormalizerOptions`, `ConvosoOptions`, and others — the convention is enforced by the dump mechanism calling `Clone()` rather than serializing the original object directly.

---

## Evidence: IConfigureOptions for Complex Binding

The Madera address normalization subsystem uses `IConfigureOptions<T>` for cases where simple `BindConfiguration()` is insufficient:

```csharp
public class LobNormalizerOptionsConfigurator : IConfigureOptions<LobNormalizerOptions>
{
    private readonly IConfiguration _config;
    public LobNormalizerOptionsConfigurator(IConfiguration config)
        => _config = config;

    public void Configure(LobNormalizerOptions options)
    {
        _config.GetSection(LobNormalizerOptions.Section).Bind(options);
    }
}
```

Registered via `services.ConfigureOptions<LobNormalizerOptionsConfigurator>()` in the `LobRegistry`. This pattern is used when the options class needs DI-resolved dependencies during configuration or when the binding logic is more complex than a simple section map.

### IConfigureNamedOptions for Framework Integration

The JWT authentication configuration uses `IConfigureNamedOptions<JwtBearerOptions>` to configure ASP.NET Core's authentication middleware through the options system rather than inline lambdas:

```csharp
public sealed class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtSettings _settings;

    public ConfigureJwtBearerOptions(JwtSettings settings) => _settings = settings;

    public void Configure(JwtBearerOptions options)
    {
        options.TokenValidationParameters = new()
        {
            ClockSkew = TimeSpan.FromMinutes(_settings.AllowedClockSkewMinutes),
            ValidateIssuer = true,
            // ...
            LifetimeValidator = LifetimeValidator,
        };
    }

    public void Configure(string? name, JwtBearerOptions options) => Configure(options);
}
```

This separates JWT configuration logic from the DI registration site and allows the configurator to inject its own dependencies (`JwtSettings`), which a simple lambda-based `.Configure()` call cannot do cleanly.

---

## Evidence: Options Consumption Patterns

### Options in MassTransit Consumer Definitions

The `NormalizeAddressConsumerDefinition` injects `IOptions<LobNormalizerOptions>` to configure MassTransit's batching, rate limiting, and retry policies from the same options class that configures the LOB API client:

```csharp
public sealed class NormalizeAddressConsumerDefinition : ConsumerDefinition<NormalizeAddressConsumer>
{
    private readonly IOptions<LobNormalizerOptions> _options;

    public NormalizeAddressConsumerDefinition(IOptions<LobNormalizerOptions> options)
        => _options = options;

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<NormalizeAddressConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        var current = _options.Value;
        endpointConfigurator.ConcurrentMessageLimit = current.BatchSize * current.AddressConcurrencyLimit;
        endpointConfigurator.PrefetchCount = current.BatchSize * current.AddressConcurrencyLimit;

        consumerConfigurator.Options<BatchOptions>(conf =>
        {
            conf.MessageLimit = current.BatchSize;
            conf.ConcurrencyLimit = current.AddressConcurrencyLimit;
            conf.TimeLimit = current.AddressBatchInterval;
        });

        endpointConfigurator.UseRateLimit(current.RateLimit, current.RateInterval);
        endpointConfigurator.UseMessageRetry(conf =>
        {
            conf.Handle<TooManyRequestsException>();
            conf.Interval(current.RetryLimit, current.RetryDelay);
        });
    }
}
```

This demonstrates options driving not just application logic but infrastructure behavior — batch sizes, concurrency limits, rate limits, and retry policies are all configuration-driven from a single options class.

### Resolve-Time Factory Pattern with Options

The `DataflowsRegistry` resolves options at service-resolution time inside factory lambdas to select pipeline implementations:

```csharp
services.AddScoped(ctx =>
{
    var options = ctx.GetRequiredService<IOptions<TOptions>>().Value;
    var provider = options.Pipeline?.Sink ?? PipelineSinkProviders.Bulk;

    return Create(provider);

    IPipelineSink<TPipelineData> Create(PipelineSinkProviders p) => p switch
    {
        PipelineSinkProviders.Null => ActivatorUtilities.CreateInstance<NullPipelineSink<TPipelineData>>(ctx),
        PipelineSinkProviders.Console => ActivatorUtilities.CreateInstance<ConsoleLoggingPipelineSink<TPipelineData>>(ctx),
        _ => Resolve(p)
    };
});
```

---

## Evidence: Options Hierarchy with Inheritance

The address normalization subsystem demonstrates options class inheritance. `AddressNormalizerOptions` is a base class with shared rate/batch settings, and `LobNormalizerOptions` extends it with provider-specific fields:

```csharp
public class AddressNormalizerOptions
{
    public required int RateLimit { get; init; }
    public required TimeSpan RateInterval { get; init; }
    public required int BatchSize { get; init; }
}

public sealed class LobNormalizerOptions : AddressNormalizerOptions, ICloneable
{
    public static string Section => "Lob";

    [SensitiveData]
    public required string ApiAuthorization { get; init; }
    public required TimeSpan RequestTimeout { get; init; }
    public required Uri RequestBaseUri { get; init; }
}
```

Code that only needs rate/batch settings can depend on `IOptions<AddressNormalizerOptions>`, while code that needs the full LOB API configuration depends on `IOptions<LobNormalizerOptions>`.

---

## Key Files

- `madera-apps:Madera/Madera.Common/Startup/OptionsObjects.cs` — BusTransportOptions, MessageDataOptions, MessageSchedulingOptions, ExtendedJobOptions with ICloneable + SensitiveData
- `madera-apps:Madera/Madera.AddressNormalization/Configuration/LobNormalizerOptionsConfigurator.cs` — IConfigureOptions<T> explicit implementation
- `madera-apps:Madera/Madera.AddressNormalization/Configuration/LobNormalizerOptions.cs` — Options with rate limiting, retry, and API config
- `madera-apps:Madera/Madera.Common.AddressNormalization.Lob/Registries/LobRegistry.cs` — Options inheritance: AddressNormalizerOptions base, LobNormalizerOptions derived
- `madera-apps:Madera/Madera.UI.Server/Identity/JwtScheme/ConfigureJwtBearerOptions.cs` — IConfigureNamedOptions for JWT bearer configuration
- `madera-apps:Madera/Madera.AddressNormalization/Consumers/NormalizeAddressConsumer.cs` — Options driving MassTransit batch/rate/retry config
