# Aetherweave.Core

Shared resources used by other Aetherweave packages — configuration binding with startup validation, and the exceptions that go with it.

## Features

- **Configuration Binding** - Bind and validate strongly-typed options from `IConfiguration`
- **Startup Validation** - Invalid configuration fails fast via `ValidateOnStart()`
- **Data Annotations Support** - Use standard `[Required]`, `[Range]`, etc. on your options classes
- **Named Options** - Bind multiple sections to the same options type
- **Descriptive Exceptions** - Clear errors when a section is missing or fails validation

## Installation

```bash
dotnet add package Zwedze.Aetherweave.Core
```

## Quick Start

### 1. Register options at startup

Binds a configuration section to `TOptions`, validates data annotations, and re-validates on every access via `IOptions<TOptions>`. Throws `ConfigurationNotFoundException` immediately if the section doesn't exist.

```csharp
public sealed record JwtBearerOptions
{
    [Required(AllowEmptyStrings = false)] public required string Authority { get; init; }
    [Required(AllowEmptyStrings = false)] public required string Audience { get; init; }
}

ConfigurationLoader.RegisterOptions<JwtBearerOptions>(
    services,
    configuration,
    "Aetherweave:Security:JwtBearer");
```

**Named options** - bind several sibling sections (e.g. one per named HttpClient) to the same type:

```csharp
var sections = configuration.GetSection("Aetherweave:HttpClients").GetChildren();

ConfigurationLoader.RegisterOptions<HttpClientOptions>(services, sections);
```

### 2. Read options eagerly

Use `GetOptions<T>` when you need the bound and validated value immediately (e.g. inside another extension method that configures a third-party library), rather than through DI's `IOptions<T>`.

```csharp
var options = ConfigurationLoader.GetOptions<JwtBearerOptions>(
    configuration,
    "Aetherweave:Security:JwtBearer");
```

Throws `ConfigurationInvalidException` if any data annotation fails, with all validation errors joined into the message.

### 3. Exceptions

| Type | When |
|---|---|
| `ConfigurationNotFoundException` | The requested section does not exist in `IConfiguration` |
| `ConfigurationInvalidException` | The bound options object fails data annotation validation |

```csharp
try
{
    var options = ConfigurationLoader.GetOptions<JwtBearerOptions>(configuration, "Aetherweave:Security:JwtBearer");
}
catch (ConfigurationNotFoundException ex)
{
    // "Configuration section 'Aetherweave:Security:JwtBearer' not found. Ensure your appsettings contains the required configuration."
}
catch (ConfigurationInvalidException ex)
{
    // "Configuration section 'Aetherweave:Security:JwtBearer' is invalid. Errors: ..."
}
```

## API Reference

| Member | Signature |
|---|---|
| `ConfigurationLoader.RegisterOptions<T>` | `(IServiceCollection, IConfiguration, string sectionName, string? optionName = null)` — DI-based, validated on start |
| `ConfigurationLoader.RegisterOptions<T>` | `(IServiceCollection, IEnumerable<IConfigurationSection>)` — one named option per section |
| `ConfigurationLoader.GetOptions<T>` | `(IConfiguration, string sectionName) → T` — bound and validated immediately |
