# Zwedze.Aetherweave.Core

Shared building block of the Aetherweave packages: **strongly-typed configuration binding with validation**, and the exceptions it throws.

Every configuration-driven Aetherweave package (`Http`, `Grpc`, `Data.Relational`, `Security.*`) reads its settings through it. You can also use it for your own options classes: it is a public, stable API.

```bash
dotnet add package Zwedze.Aetherweave.Core
```

---

## Why this package exists

| Problem | What Core gives you |
|---|---|
| A missing configuration section is noticed only when a request fails | `ConfigurationNotFoundException` as soon as the options are registered or read |
| Invalid values (empty URL, negative timeout) fail deep inside a third-party library | Data annotations + `IValidatableObject` validated at startup, with every error listed |
| Each package binds and validates configuration in its own way | One loader with two explicit modes: registered in DI, or read immediately |

---

## Public API at a glance

Namespace `Zwedze.Aetherweave.Core.Configurations`:

| Member | Use it when | Validation | Failure |
|---|---|---|---|
| `ConfigurationLoader.RegisterOptions<T>(IServiceCollection services, IConfiguration configuration, string sectionName, string? optionName = null)` | The options are consumed through DI (`IOptions<T>`, `IOptionsMonitor<T>`) | When the host starts (`ValidateOnStart`) | Missing section: `ConfigurationNotFoundException`, **immediately**. Invalid values: `OptionsValidationException`, **at host start** |
| `ConfigurationLoader.RegisterOptions<T>(IServiceCollection services, IEnumerable<IConfigurationSection> sections)` | Several sibling sections map to the same type, one named option per section | When the host starts | Invalid values: `OptionsValidationException`, at host start. An empty list registers nothing and **does not throw** |
| `ConfigurationLoader.GetOptions<T>(IConfiguration configuration, string sectionName)` | The value is needed right now, outside DI (typically to configure a third-party library during registration) | **Immediately** | `ConfigurationNotFoundException` or `ConfigurationInvalidException`, immediately |

Namespace `Zwedze.Aetherweave.Core.Configurations.Exceptions`:

| Exception | Thrown by | Properties |
|---|---|---|
| `ConfigurationNotFoundException` | `RegisterOptions` (single section) and `GetOptions`, when the section does not exist | `SectionName` |
| `ConfigurationInvalidException` | `GetOptions` only, when the section cannot be bound to `T` or fails validation | `SectionName`, `Errors` (every validation message) |

`RegisterOptions` never throws `ConfigurationInvalidException`: invalid registered options surface as .NET's `OptionsValidationException` (`Microsoft.Extensions.Options`).

---

## Core rules

1. **Pick the mode by consumer.** Consumed through DI → `RegisterOptions`. Needed during registration (to configure another library) → `GetOptions`. Do not call both for the same section.
2. **Declare the rules on the options type.** Use `[Required(AllowEmptyStrings = false)]`, `[Range]`, `[Url]`... on properties, and implement `IValidatableObject` for rules that span several properties or need parsing (URI format, positive `TimeSpan`).
3. **Use `sealed record` options with `required` / `init` properties**, defaults set in the initializer (`public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);`).
4. **Only top-level properties are validated.** Data annotations on a nested object are not evaluated: validate nested values from the parent's `IValidatableObject.Validate`, or flatten the options.
5. **Section paths use `:` separators** (`Aetherweave:HttpClients:OrderService`). Aetherweave packages keep their settings under the `Aetherweave:` prefix; use your own prefix for your options.
6. **Named options are case-sensitive.** A named option registered with `optionName: "OrderService"` (or from a section key) must be resolved with exactly that name: `IOptionsMonitor<T>.Get("OrderService")`.
7. **Startup validation needs a host.** `ValidateOnStart` runs when `IHost` starts (ASP.NET Core, worker service, generic host). With a bare `ServiceCollection`, validation runs only on the first resolution of the options.
8. **`IOptions<T>` is validated once** (then cached). `IOptionsMonitor<T>` re-binds and re-validates when the configuration source reloads.

---

## Canonical examples

### Options type

```csharp
public sealed record PaymentServiceOptions : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    public required string BaseAddress { get; init; }

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.TryCreate(BaseAddress, UriKind.Absolute, out _))
        {
            yield return new ValidationResult("BaseAddress must be a valid absolute URI", [nameof(BaseAddress)]);
        }

        if (Timeout <= TimeSpan.Zero)
        {
            yield return new ValidationResult("Timeout must be greater than zero", [nameof(Timeout)]);
        }
    }
}
```

```json
{
  "MyApp": {
    "PaymentService": {
      "BaseAddress": "https://payments.example.com",
      "Timeout": "00:00:10"
    }
  }
}
```

### Registered in DI (validated at host start)

```csharp
ConfigurationLoader.RegisterOptions<PaymentServiceOptions>(services, configuration, "MyApp:PaymentService");

// Consumer
internal sealed class PaymentServiceClient(IOptions<PaymentServiceOptions> options) { ... }
```

### Named options (one per client)

```csharp
// Single named section (what Zwedze.Aetherweave.Http and Grpc do per client)
ConfigurationLoader.RegisterOptions<PaymentServiceOptions>(
    services, configuration, "MyApp:Clients:Payments", optionName: "Payments");

// Every child of a parent section, named after its key
ConfigurationLoader.RegisterOptions<PaymentServiceOptions>(
    services, configuration.GetSection("MyApp:Clients").GetChildren());

// Consumer: exact, case-sensitive name
var payments = optionsMonitor.Get("Payments");
```

### Read immediately (validated now)

```csharp
public static IServiceCollection AddPayments(this IServiceCollection services, IConfiguration configuration)
{
    var options = ConfigurationLoader.GetOptions<PaymentServiceOptions>(configuration, "MyApp:PaymentService");

    services.AddThirdPartyPaymentSdk(sdk =>
    {
        sdk.Endpoint = new Uri(options.BaseAddress);
        sdk.Timeout = options.Timeout;
    });

    return services;
}
```

### Handling the exceptions

Configuration errors are deployment errors: let them crash the startup. Catch them only to add context or to log them in a specific format.

```csharp
try
{
    var options = ConfigurationLoader.GetOptions<PaymentServiceOptions>(configuration, "MyApp:PaymentService");
}
catch (ConfigurationNotFoundException ex)
{
    logger.LogCritical("Missing configuration section {Section}", ex.SectionName);
    throw;
}
catch (ConfigurationInvalidException ex)
{
    logger.LogCritical("Invalid configuration section {Section}: {Errors}", ex.SectionName, ex.Errors);
    throw;
}
```

---

## Used by

| Package | Mode | Section |
|---|---|---|
| `Zwedze.Aetherweave.Http` | `RegisterOptions`, named per client | `Aetherweave:HttpClients:{clientName}` |
| `Zwedze.Aetherweave.Grpc` | `RegisterOptions`, named per client | `Aetherweave:GrpcClients:{clientName}` |
| `Zwedze.Aetherweave.Data.Relational` | `RegisterOptions` | `Aetherweave:DataRelational` |
| `Zwedze.Aetherweave.Security.Jwt` | `GetOptions` | `Aetherweave:Security:JwtBearer` |
| `Zwedze.Aetherweave.Security.Oidc` | `GetOptions` | `Aetherweave:Security:Oidc` |
| `Zwedze.Aetherweave.Security.ClientCredentials` | `GetOptions`, once per scheme | `Aetherweave:Security:ClientCredentials:{schemeName}` |

---

## Anti-patterns

| Don't | Do instead |
|---|---|
| `configuration.GetSection("X").Get<T>()!` | `ConfigurationLoader.GetOptions<T>(configuration, "X")`: missing and invalid sections fail with a clear exception |
| Catching `ConfigurationInvalidException` around `RegisterOptions` | Invalid registered options throw `OptionsValidationException` at host start |
| Validating options by hand in the consumer | Put the rules on the options type (annotations / `IValidatableObject`) |
| `[Required]` on a property of a nested object, expecting it to be checked | Validate nested values from the parent's `IValidatableObject.Validate` |
| `optionsMonitor.Get("orderservice")` for a section named `OrderService` | Use the exact, case-sensitive name |
| Catching configuration exceptions and continuing with defaults | Let startup fail: the deployment is broken |
