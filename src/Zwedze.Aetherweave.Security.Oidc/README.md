# Aetherweave.Security.Oidc

Configuration-driven OIDC authentication for Blazor WebAssembly clients, with authorized `HttpClient` registration built on top of `Zwedze.Aetherweave.Http`.

## Features

- **One-Line Registration** - Configure `AddOidcAuthentication` from `appsettings.json` instead of hand-wiring `ProviderOptions`
- **Startup Validation** - Missing or invalid configuration throws immediately via `Zwedze.Aetherweave.Core`
- **Authorized HTTP Clients** - Register Aetherweave typed HttpClients that automatically attach the OIDC access token
- **Type-Safe Options** - `AetherweaveOidcOptions` validated via data annotations

## Installation

```bash
dotnet add package Zwedze.Aetherweave.Security.Oidc
```

## Configuration (`Aetherweave:Security:Oidc`)

| Key | Notes |
|---|---|
| `Authority` | *Required.* The identity provider's URL |
| `ClientId` | *Required.* The registered client identifier |
| `ResponseType` | *Required.* OIDC response type, e.g. `code` |
| `DefaultScopes` | *Required.* Scopes requested on every sign-in |
| `RedirectUri` | *Required.* Post-login redirect, e.g. `authentication/login-callback` |
| `PostLogoutRedirectUri` | *Required.* Post-logout redirect, e.g. `authentication/logout-callback` |

```json
{
  "Aetherweave": {
    "Security": {
      "Oidc": {
        "Authority": "https://identity.example.com",
        "ClientId": "orders-web",
        "ResponseType": "code",
        "DefaultScopes": [ "openid", "profile", "orders-api" ],
        "RedirectUri": "authentication/login-callback",
        "PostLogoutRedirectUri": "authentication/logout-callback"
      }
    }
  }
}
```

## Quick Start

### 1. Register OIDC client authentication

```csharp
builder.Services.AddAetherweaveOidcClientAuthentication(builder.Configuration);

// Or with a custom section name
builder.Services.AddAetherweaveOidcClientAuthentication(builder.Configuration, "MyApp:Oidc");
```

This reads and validates `AetherweaveOidcOptions` from configuration, then calls `AddOidcAuthentication(...)` mapping `Authority`, `ClientId`, `ResponseType`, `RedirectUri`, `PostLogoutRedirectUri`, and appending every entry of `DefaultScopes` to `ProviderOptions.DefaultScopes`.

### 2. Register authorized HttpClients

`AddAetherweaveOidcHttpClients` returns a builder whose typed clients are registered via `Zwedze.Aetherweave.Http` and automatically attach the current OIDC access token through `AuthorizationMessageHandler`.

```csharp
builder.Services
    .AddAetherweaveOidcHttpClients(authorizedUrls: ["https://api.example.com"])
    .AddAetherweaveHttpClient<IOrderServiceClient, OrderServiceClient>(
        builder.Configuration,
        "OrderService");
```

`authorizedUrls` are the base addresses the access token is allowed to be attached to — pass the same URLs registered with `AuthorizationMessageHandler` (matches Blazor WebAssembly's standard `WithAuthorizationMessageHandler` set up in `Program.cs`).

### 3. Attach auth to any other `IHttpClientBuilder`

For clients not registered through `AddAetherweaveOidcHttpClients`, the `WithAuth` extension attaches the same handler directly:

```csharp
builder.Services
    .AddHttpClient<IOrderServiceClient, OrderServiceClient>()
    .WithAuth(["https://api.example.com"]);
```

## API Reference

| Member | Signature |
|---|---|
| `AddAetherweaveOidcClientAuthentication` | `(this IServiceCollection, IConfiguration, string sectionName = "Aetherweave:Security:Oidc") → IServiceCollection` |
| `AddAetherweaveOidcHttpClients` | `(this IServiceCollection, IEnumerable<string> authorizedUrls, string sectionName = "Aetherweave:Security:Oidc") → OidcHttpClientBuilder` |
| `OidcHttpClientBuilder.AddAetherweaveHttpClient<TInterface, TImplementation>` | `(IConfiguration, string clientName) → IHttpClientBuilder` — registers via `Zwedze.Aetherweave.Http` and attaches `AuthorizationMessageHandler` |
| `IHttpClientBuilder.WithAuth` | `(IEnumerable<string> authorizedUrls) → IHttpClientBuilder` — attaches `AuthorizationMessageHandler` to an existing builder |
