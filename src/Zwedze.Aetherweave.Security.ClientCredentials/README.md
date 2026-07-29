# Aetherweave.Security.ClientCredentials

Backend-to-backend authentication: **your service calling other APIs as itself**, with no user involved
(OAuth2 client credentials). Token acquisition, caching, and renewal-on-401 are handled entirely by
`Duende.AccessTokenManagement` — not a custom re-implementation.

For the other two Aetherweave auth cases, see their own packages:

| Case | Package |
|---|---|
| Protecting your own API by validating incoming JWTs | `Zwedze.Aetherweave.Security.Jwt` |
| Interactive user login for a Blazor WebAssembly UI | `Zwedze.Aetherweave.Security.Oidc` |

A service can be a client of many different APIs, each behind a different identity provider — register
one named scheme per downstream API.

## Installation

```bash
dotnet add package Zwedze.Aetherweave.Security.ClientCredentials
```

## Configuration (`Aetherweave:Security:ClientCredentials`)

| Key | Notes |
|---|---|
| `TokenEndpoint` | *Required.* Absolute URI of the IDP's token endpoint |
| `ClientId` | *Required.* |
| `ClientSecret` | *Required.* Never commit real values — use environment-specific configuration or a secret store |
| `Scope` | Optional |

```json
{
  "Aetherweave": {
    "Security": {
      "ClientCredentials": {
        "orders-api": {
          "TokenEndpoint": "https://identity.example.com/connect/token",
          "ClientId": "orders-service",
          "ClientSecret": "secret",
          "Scope": "orders.api"
        },
        "payments-api": {
          "TokenEndpoint": "https://payments-identity.example.com/connect/token",
          "ClientId": "orders-service",
          "ClientSecret": "another-secret"
        }
      }
    }
  }
}
```

## Quick Start

### 1. Register named schemes once

```csharp
services.AddAetherweaveClientCredentialsAuthentication(configuration);

// Or with a custom section name
services.AddAetherweaveClientCredentialsAuthentication(configuration, "MyApp:ClientCredentials");
```

Configure zero, one, or many named schemes — nothing is registered globally until an `HttpClient` opts
into a scheme.

### 2. Opt in per HttpClient

```csharp
services.AddAetherweaveHttpClient<IOrderServiceClient, OrderServiceClient>(configuration, "OrderService")
    .WithClientCredentialsAuthentication("orders-api");
```

## Behavior notes

- **Failures don't throw.** If `Duende.AccessTokenManagement` can't acquire a token, it logs a warning
  and sends the request *without* a token rather than throwing — this is Duende's own documented
  behavior, not something this library changes.
- **Never commit `ClientSecret` values** — use environment-specific configuration or a secret store.

## API Reference

| Member | Signature |
|---|---|
| `AddAetherweaveClientCredentialsAuthentication` | `(this IServiceCollection, IConfiguration, string sectionName = "Aetherweave:Security:ClientCredentials") → IServiceCollection` |
| `IHttpClientBuilder.WithClientCredentialsAuthentication` | `(string schemeName) → IHttpClientBuilder` — attaches the client-credentials token handler to an `HttpClient` |
