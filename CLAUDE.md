# CLAUDE.md — Zwedze.Aetherweave

Guidance for working with the Aetherweave solution. Read this before making changes.

## Solution overview

Aetherweave is a collection of NuGet packages for building .NET 10 applications with DDD and clean-architecture patterns. All source projects live under `src/`; tests under `tests/`.

```
Zwedze.Aetherweave.sln
├── src/
│   ├── Zwedze.Aetherweave.SharedKernel          # Domain primitives: Id<T>, Code<T>, AggregateRoot, DomainEvents
│   ├── Zwedze.Aetherweave.Application           # CQRS interfaces, domain event dispatcher, ResponseWrapper, ErrorFactory
│   ├── Zwedze.Aetherweave.Data                  # IUnitOfWork + IUnitOfWorkFactory abstractions only
│   ├── Zwedze.Aetherweave.Data.Relational       # EF Core UoW implementation, auto-config, health checks
│   ├── Zwedze.Aetherweave.Core                  # Shared config binding + startup validation used by other packages
│   ├── Zwedze.Aetherweave.Http                  # Typed HttpClient: profiling, content tracing, error handling (no auth)
│   ├── Zwedze.Aetherweave.Grpc                  # Typed gRPC clients via Grpc.Net.ClientFactory: config-bound address/timeout (no auth)
│   ├── Zwedze.Aetherweave.Security.Jwt          # Protect your own API: validate incoming JWTs against one IDP
│   ├── Zwedze.Aetherweave.Security.ClientCredentials  # Backend-to-backend: your service as its own OAuth2 client
│   ├── Zwedze.Aetherweave.Security.Oidc         # Interactive user login (Authorization Code + PKCE) for Blazor WebAssembly
│   ├── Zwedze.Aetherweave.IdentityGenerators    # Snowflake-based distributed ID generation
│   ├── Zwedze.Aetherweave.Analyzers             # Roslyn analyzers: Id/Code duplication detection (compile-time errors)
│   └── Zwedze.Aetherweave.Generators            # Source generator: [SmartEnum] boilerplate generation
└── tests/
    ├── Zwedze.Aetherweave.Generators.Test                    # NUnit tests for SmartEnumGenerator
    ├── Zwedze.Aetherweave.Grpc.Test                          # NUnit tests for Grpc
    └── Zwedze.Aetherweave.Security.ClientCredentials.Test    # NUnit tests for Security.ClientCredentials
```

**Solution folders in the .sln:** Design (Application + SharedKernel), Data, Http, Grpc (Grpc + Grpc.Test), Security (Core, Security.Jwt, Security.ClientCredentials, Security.Oidc), IdGenerator, Common (Analyzers + Generators + Test), IntegrationTest (Test.Api, Test.Blazor, Test.Console).

## Authentication model — which package for which case

Aetherweave splits authentication into three packages with no overlap. Pick by **what you're building**, not by protocol name:

| You are... | Use | Notes |
|---|---|---|
| Protecting your own API | `Security.Jwt` | Validates incoming JWTs against one IDP. Works identically no matter which flow issued the token (client-credentials or a PKCE user flow) — this package doesn't care. |
| Calling other APIs as your service (no user) | `Security.ClientCredentials` | Backend-to-backend. A service can call many downstream APIs behind different IDPs — one named scheme per API. |
| Logging a user in from a Blazor WebAssembly UI | `Security.Oidc` | Authorization Code + PKCE, browser-hosted. |

All three live under the `Aetherweave:Security:*` configuration prefix. `Http` itself carries no auth code and no auth-related dependency — it's the generic `HttpClient` plumbing that `Security.ClientCredentials` and `Security.Oidc` both build on. `Grpc` is the gRPC analogue of `Http`: `Grpc.Net.ClientFactory`'s `AddGrpcClient<TClient>()` returns a plain `IHttpClientBuilder`, so `Security.ClientCredentials.WithClientCredentialsAuthentication(schemeName)` composes onto gRPC clients with zero code changes to that package. Only the client-credentials case is supported for gRPC today — `Security.Oidc` does not build on `Grpc`.

## Project dependency graph

```
SharedKernel  ←─── Application
SharedKernel  ←─── IdentityGenerators
Data          ←─── Data.Relational

Core          ←─── Http
Core          ←─── Grpc
Core          ←─── Security.Jwt
Core          ←─── Security.ClientCredentials  ←── Http
Core          ←─── Security.Oidc               ←── Http

Analyzers              (standalone, netstandard2.0)
Generators             (standalone, netstandard2.0; consuming project needs SharedKernel)
```

## Cross-cutting conventions

- **Target framework:** .NET 10.0 for all library projects except Analyzers and Generators (netstandard2.0 — required by Roslyn).
- **Nullable + implicit usings:** enabled everywhere.
- **C# 14 extension methods:** `ServiceCollectionExtensions` in `Data.Relational` and `Http` use the new `extension(TypeName x) { }` syntax. Match this style when adding new extension methods.
- **Analyzers:** Roslynator (Analyzers + CodeAnalysis + Formatting) applied in every project; style violations are build errors. Match existing formatting exactly.
- **`[UsedImplicitly]`** (JetBrains.Annotations, private asset): decorates public interfaces and factory methods to silence false-positive "unused" warnings from Roslyn/Rider.
- **Error codes:** `SCREAMING_SNAKE_CASE` strings. The constant `Error.UnmanagedErrorCode = "__unmanaged_error"` is reserved for unhandled exceptions.
- **Versioning:** managed by `GitVersion.yml` (semver from git tags). Never edit `<Version>` in `.csproj` files manually.
- **Packaging:** all `src/` projects are `<IsPackable>true</IsPackable>`. Shared icon at repo root `Icon.png`; each project bundles its own `README.md` as the NuGet readme (except `Data`, `Analyzers`, `Generators` which don't yet declare `<PackageReadmeFile>`).
- **Analyzers / Generators packaging:** `<DevelopmentDependency>true</DevelopmentDependency>`, `<IncludeBuildOutput>false</IncludeBuildOutput>`, assembly packed under `analyzers/dotnet/cs/`.

---

## Zwedze.Aetherweave.SharedKernel

**README:** [`src/Zwedze.Aetherweave.SharedKernel/README.md`](src/Zwedze.Aetherweave.SharedKernel/README.md)

DDD primitives. Every other domain-touching project depends on this.

| Type | Kind | Key facts |
|---|---|---|
| `Id<T>` | `readonly struct` | Wraps `long`; rejects ≤ 0. `Id<T>.From(long)` factory, implicit to `long`, explicit from `long`. |
| `Code<T>` | `readonly struct` | Wraps `string`; rejects null/whitespace. `Code<T>.From(string)`, implicit to `string`, explicit from `string`. |
| `AggregateRoot<T>` | abstract class | Requires `Id<T>` + `Code<T>`. Equality by `Id`. Call `RaiseDomainEvent(IDomainEvent)` inside; consumers call `PopDomainEvents()` to drain. |
| `IAggregateRoot<T>` | interface | Exposes `Id`, `Code`, `PopDomainEvents()`. |
| `IHasDomainEvents` | interface | `ImmutableArray<IDomainEvent> PopDomainEvents()` — drains and clears the event list. |
| `BusinessException` | abstract class | `message` + `errorCode` (string). Extend for domain-specific errors. |
| `IDomainEvent` | interface | `Guid Id`, `DateTimeOffset OccurredOn`. |
| `DomainEvent` | abstract record | Default-initialises `Id = Guid.NewGuid()`, `OccurredOn = UtcNow`. Inherit for concrete events. |
| `IDomainEventHandler<TEvent>` | interface | `Task HandleAsync(TEvent, CancellationToken)`. |

---

## Zwedze.Aetherweave.Application

**README:** [`src/Zwedze.Aetherweave.Application/README.md`](src/Zwedze.Aetherweave.Application/README.md)

Application-layer orchestration: CQRS contracts, domain event dispatch, result type, error factory.

### CQRS interfaces

```csharp
ICommandHandler<TRequest, TResponse>  // Handle(TRequest, CT) → Task<ResponseWrapper<TResponse>>
ICommandHandler<TRequest>             // Handle(TRequest, CT) → Task<ResponseWrapper>
IQueryHandler<TRequest, TResponse>    // Handle(TRequest, CT) → Task<ResponseWrapper<TResponse>>
IQueryHandler<TResponse>              // Handle(CT)           → Task<ResponseWrapper<TResponse>>
```

### ResponseWrapper

Two types: non-generic `ResponseWrapper` (void operations) and `ResponseWrapper<T>` (value-returning operations). Factory methods are on the non-generic `ResponseWrapper` class; `Success` and `Failure` nested records are `public` and can be pattern-matched:

```csharp
return result switch
{
    ResponseWrapper<Guid>.Success s => Ok(s.Value),
    ResponseWrapper<Guid>.Failure f => BadRequest(f.Error),
    _ => StatusCode(500)
};
```

Factory methods:

```csharp
ResponseWrapper.Ok()            // → ResponseWrapper         (void success)
ResponseWrapper.Ok(value)       // → ResponseWrapper<T>      (value success, type inferred)
ResponseWrapper.Fail(error)     // → ResponseWrapper         (void failure)
ResponseWrapper.Fail<T>(error)  // → ResponseWrapper<T>      (typed failure)
```

### Domain event dispatcher

```csharp
// Registration (Program.cs)
services.AddAetherweaveDomainEventDispatcher(registry =>
{
    registry.Configure<OrderCreatedEvent>()
        .AddHandler<OrderCreatedEmailHandler>()
        .AddHandler<OrderCreatedSmsHandler>();

    registry.AddHandler<PaymentReceivedEvent, PaymentReceivedHandler>(); // shorthand
});

// Dispatch (always after Commit)
await uow.Commit(ct);
await eventDispatcher.DispatchAsync(aggregate, ct);
```

`IDomainEventDispatcher` is **scoped**. All registered handlers run; if any throw, an `AggregateException` is raised after all handlers complete.

### Error + ErrorFactory

```csharp
ErrorFactory.Create("CODE", "message")          // from string pair
ErrorFactory.Create(businessException)           // extracts ErrorCode + Message
ErrorFactory.Create(aggregateException)          // merges multiple BusinessExceptions
ErrorFactory.Create(anyException)               // uses UnmanagedErrorCode
```

---

## Zwedze.Aetherweave.Data

**README:** [`src/Zwedze.Aetherweave.Data/README.md`](src/Zwedze.Aetherweave.Data/README.md)

Pure abstractions — no EF Core dependency.

| Interface | Members |
|---|---|
| `IUnitOfWork` | `SaveChanges(CT)`, `Dispose()`, `DisposeAsync()` |
| `ITransactionalUnitOfWork : IUnitOfWork` | adds `Commit(CT)`, `Rollback(CT)` |
| `IUnitOfWorkFactory` | `CreateTransactional()` → `ITransactionalUnitOfWork` |

> `IUnitOfWorkFactory` exposes **only** `CreateTransactional()`. For read-only operations, inject the `DbContext` directly — no unit of work needed.

---

## Zwedze.Aetherweave.Data.Relational

**README:** [`src/Zwedze.Aetherweave.Data.Relational/README.md`](src/Zwedze.Aetherweave.Data.Relational/README.md)

EF Core implementation of the Data abstractions.

### Registration

```csharp
services.AddAetherweaveData<ApplicationDbContext>(
    configuration,
    (builder, options) => builder.UseNpgsql(
        configuration.GetConnectionString(options.ConnectionStringName)),
    sectionName: "Aetherweave:DataRelational",  // optional, this is the default
    addHealthCheck: true                          // optional, defaults to true
);
```

Uses C# 14 `extension(IServiceCollection services)` syntax internally.

### Configuration (`Aetherweave:DataRelational`)

| Key | Default | Notes |
|---|---|---|
| `ConnectionStringName` | *required* | Key in `ConnectionStrings` section |
| `EnableDetailedErrors` | `true` | Disable in production |
| `EnableSensitiveDataLogging` | `false` | Never enable in production |
| `NoTrackingAsDefaultTrackingStrategy` | `true` | No-tracking by default |

### Transactional UoW behaviour

- Transaction is **lazy**: starts on the first call to `SaveChanges`.
- Disposing without calling `Commit` logs a warning and rolls back automatically.
- `Commit` throws `TransactionAlreadyCommittedException` if called twice.
- `Commit` throws `NoTransactionException` if `SaveChanges` was never called.

### Exceptions

| Type | When |
|---|---|
| `ConfigurationNotFoundException` | Config section absent at startup |
| `TransactionAlreadyCommittedException` | `Commit()` called more than once |
| `NoTransactionException` | `Commit()` before any `SaveChanges()` |
| `TransactionNotCommittedException` | (logged as warning on disposal) |

---

## Zwedze.Aetherweave.Http

**README:** [`src/Zwedze.Aetherweave.Http/README.md`](src/Zwedze.Aetherweave.Http/README.md)

Type-safe, configuration-driven `HttpClient` setup. Purely generic — no auth code, no auth-related package dependency. See [Authentication model](#authentication-model--which-package-for-which-case) above for the packages that build auth on top of this one.

### Registration

```csharp
services.AddAetherweaveHttpClient<IOrderServiceClient, OrderServiceClient>(
    configuration,
    "OrderService",                    // client name — matches config key
    sectionName: "Aetherweave:HttpClients"  // optional, this is the default
);
```

Fluent extensions on `IHttpClientBuilder` (C# 14 extension syntax):

```csharp
builder.WithHandler<CorrelationIdHandler>()         // custom DelegatingHandler
       .WithErrorHandler<OrderServiceErrorHandler>(); // implements IHttpErrorHandler
```

### Configuration (`Aetherweave:HttpClients:{clientName}`)

| Key | Default | Notes |
|---|---|---|
| `BaseAddress` | *required* | Must be an absolute URI |
| `Timeout` | `00:00:30` | Must be > 0 |
| `EnableProfiling` | `false` | Logs method, URI, duration, status — safe for production |
| `EnableContentTracing` | `false` | Logs response body — **never in production** |
| `MaxContentLogSize` | `10000` | Bytes; content truncated if larger |

### Built-in handlers (always added, conditionally active)

- **`ProfilingHandler`** — active when `EnableProfiling = true`; minimal overhead.
- **`ContentTracingHandler`** — active when `EnableContentTracing = true`; reads full response body into memory — development only.
- **`HttpErrorResponseHandler`** — active when `WithErrorHandler<T>()` is called; delegates to `IHttpErrorHandler.HandleError`.

---

## Zwedze.Aetherweave.Grpc

**README:** [`src/Zwedze.Aetherweave.Grpc/README.md`](src/Zwedze.Aetherweave.Grpc/README.md)

Type-safe, configuration-driven gRPC client registration via `Grpc.Net.ClientFactory`. Purely generic — no auth code, no auth-related package dependency. Same split as `Http`: see [Authentication model](#authentication-model--which-package-for-which-case) above.

### Registration

```csharp
services.AddAetherweaveGrpcClient<Greeter.GreeterClient>(
    configuration,
    "GreeterService",                  // client name — matches config key
    sectionName: "Aetherweave:GrpcClients"  // optional, this is the default
);
```

`AddAetherweaveGrpcClient<TClient>` returns a plain `IHttpClientBuilder` (same as `Grpc.Net.ClientFactory`'s own `AddGrpcClient<TClient>`) — only one generic parameter, unlike `Http`'s `<TInterface, TImplementation>`, since gRPC-generated clients are concrete classes with no separate interface.

### Configuration (`Aetherweave:GrpcClients:{clientName}`)

| Key | Default | Notes |
|---|---|---|
| `Address` | *required* | Must be an absolute URI |
| `Timeout` | `00:00:30` | Must be > 0 |

### Authentication

No built-in handlers — compose `Security.ClientCredentials.WithClientCredentialsAuthentication(schemeName)` directly onto the returned `IHttpClientBuilder`:

```csharp
services.AddAetherweaveGrpcClient<Greeter.GreeterClient>(configuration, "GreeterService")
    .WithClientCredentialsAuthentication("greeter-api");
```

---

## Zwedze.Aetherweave.Core

**README:** [`src/Zwedze.Aetherweave.Core/README.md`](src/Zwedze.Aetherweave.Core/README.md)

Shared resources used by other Aetherweave packages — configuration binding with startup validation, and the exceptions that go with it. Every `Security.*` package and `Http` depend on this.

```csharp
ConfigurationLoader.RegisterOptions<TOptions>(services, configuration, sectionName, optionName: null); // DI-bound, validated on start
ConfigurationLoader.RegisterOptions<TOptions>(services, sections);                                     // one named option per section (e.g. per HttpClient/scheme)
ConfigurationLoader.GetOptions<TOptions>(configuration, sectionName);                                   // bound + validated immediately, outside DI
```

| Exception | When |
|---|---|
| `ConfigurationNotFoundException` | The requested section does not exist in `IConfiguration` |
| `ConfigurationInvalidException` | The bound options object fails data annotation validation |

---

## Zwedze.Aetherweave.Security.Jwt

**README:** [`src/Zwedze.Aetherweave.Security.Jwt/README.md`](src/Zwedze.Aetherweave.Security.Jwt/README.md)

Protects your own API: configuration-driven JWT Bearer authentication against a single IDP. Works identically regardless of which flow produced the token — this package only validates.

```csharp
services.AddAetherweaveJwtBearerAuthentication(configuration); // reads Aetherweave:Security:JwtBearer
```

| Key | Notes |
|---|---|
| `Authority` | *Required.* Token issuer |
| `Audience` | *Required.* Expected `aud` claim |
| `RequireHttpsMetadata` | *Required.* Set `false` only for local development |

Remember to also call `app.UseAuthentication()`/`app.UseAuthorization()` and add `[Authorize]` to protected endpoints — this package only wires up the JWT Bearer handler, not authorization policies.

---

## Zwedze.Aetherweave.Security.ClientCredentials

**README:** [`src/Zwedze.Aetherweave.Security.ClientCredentials/README.md`](src/Zwedze.Aetherweave.Security.ClientCredentials/README.md)

Backend-to-backend: your service calling other APIs as itself (OAuth2 client credentials), no user involved. Built on `Duende.AccessTokenManagement`.

```csharp
services.AddAetherweaveClientCredentialsAuthentication(configuration); // reads Aetherweave:Security:ClientCredentials

services.AddAetherweaveHttpClient<IOrderServiceClient, OrderServiceClient>(configuration, "OrderService")
    .WithClientCredentialsAuthentication("orders-api");
```

Register one named scheme per downstream API — each can point at a different IDP. Token acquisition/caching/renewal is handled entirely by Duende; a failed acquisition logs a warning and sends the request without a token rather than throwing.

Also composes onto gRPC clients registered via `Zwedze.Aetherweave.Grpc` — `AddGrpcClient<TClient>` returns an `IHttpClientBuilder` too, so no code changes were needed to support that case.

---

## Zwedze.Aetherweave.Security.Oidc

**README:** [`src/Zwedze.Aetherweave.Security.Oidc/README.md`](src/Zwedze.Aetherweave.Security.Oidc/README.md)

Interactive user login (Authorization Code + PKCE) for **Blazor WebAssembly** UIs, built on `Microsoft.AspNetCore.Components.WebAssembly.Authentication`. Also registers authorized `HttpClient`s (via `Zwedze.Aetherweave.Http`) that automatically attach the OIDC access token.

```csharp
builder.Services.AddAetherweaveOidcClientAuthentication(builder.Configuration); // reads Aetherweave:Security:Oidc

builder.Services
    .AddAetherweaveOidcHttpClients(authorizedUrls: ["https://api.example.com"])
    .AddAetherweaveHttpClient<IOrderServiceClient, OrderServiceClient>(builder.Configuration, "OrderService");
```

---

## Zwedze.Aetherweave.IdentityGenerators

**README:** [`src/Zwedze.Aetherweave.IdentityGenerators/README.md`](src/Zwedze.Aetherweave.IdentityGenerators/README.md)

Snowflake-based distributed ID generation (uses `IdGen` + `Polly`). Registered as **singleton**.

### Registration

```csharp
services.AddAetherweaveGenerators(options =>
{
    options.InstanceIdType = InstanceIdType.MachineName; // or Ip
    options.EpochStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
});
```

### IIdentityGenerator

| Method | Returns | Notes |
|---|---|---|
| `GetNextUid()` | `long` | Raw 64-bit Snowflake ID |
| `CreateId<T>()` | `Id<T>` | Snowflake ID wrapped in type-safe `Id<T>` |
| `CreateCode<T>()` | `Code<T>` | **UUID v7** string (time-ordered, globally unique) wrapped in `Code<T>` |

### Instance ID strategies

| Strategy | Hostname requirement | Instance bits | Sequence bits | Max instances | IDs/ms/instance |
|---|---|---|---|---|---|
| `MachineName` | Must end with number (e.g. `WEBSERVER01`) | 10 | 12 | 1 024 | 4 096 |
| `Ip` | IPv4 required; uses last 2 octets | 16 | 6 | 65 536 | 64 |

Clock drift is handled automatically via a Polly `RetryForever` policy on `InvalidSystemClockException`.

---

## Zwedze.Aetherweave.Analyzers

**README:** [`src/Zwedze.Aetherweave.Analyzers/README.md`](src/Zwedze.Aetherweave.Analyzers/README.md)

Roslyn analyzers (netstandard2.0, `DevelopmentDependency`). Applied automatically when the package is referenced.

| Analyzer | Diagnostic ID | Severity | What it catches |
|---|---|---|---|
| `IdDuplicationAnalyzer` | `IdDuplication` | **Error** | Two fields in the same class/record using the same `(Id<T>)literal` value |
| `CodeDuplicationAnalyzer` | `CodeDuplication` | **Error** | Two fields in the same class/record using the same `(Code<T>)"value"` string |

Only inspects cast-expression field initializers (`(Id<T>)1L`, `(Code<T>)"EUR"`). Duplicate values across different types are not flagged.

---

## Zwedze.Aetherweave.Generators

**README:** [`src/Zwedze.Aetherweave.Generators/README.md`](src/Zwedze.Aetherweave.Generators/README.md)

Incremental source generator (netstandard2.0, `DevelopmentDependency`). The `[SmartEnum]` attribute is injected into the consuming project at build time — no separate attribute package needed.

### Usage

```csharp
[SmartEnum]
public sealed partial class OrderStatus   // must be partial
{
    public static readonly OrderStatus Draft     = new("draft");
    public static readonly OrderStatus Submitted = new("submitted");
}
```

### Generated members

| Member | Signature |
|---|---|
| Constructor | `private OrderStatus(string code)` |
| Code property | `public Code<OrderStatus> Code { get; }` |
| Typed lookup | `public static OrderStatus FromCode(Code<OrderStatus> code)` |
| String lookup | `public static OrderStatus FromCode(string code)` |
| Enumeration | `public static OrderStatus[] AllValues` |

Members are emitted in source declaration order. If the class is not `partial`, warning `ZFCG003` is emitted and no code is generated.

> The consuming project must reference `Zwedze.Aetherweave.SharedKernel` for the generated `Code<T>` references to resolve.
