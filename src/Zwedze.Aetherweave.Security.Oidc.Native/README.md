# Aetherweave.Security.Oidc.Native

Interactive Authorization Code + PKCE login for native, desktop, and CLI applications that can launch a
system browser (RFC 8252). This is the native-app counterpart of
[`Zwedze.Aetherweave.Security.Oidc`](../Zwedze.Aetherweave.Security.Oidc/README.md), which targets
browser-hosted Blazor WebAssembly apps instead — pick this package when the app itself is not running
inside a browser.

Built directly on `Duende.IdentityModel.OidcClient` rather than a custom re-implementation.

## Features

- **Interactive Login** - Opens a system browser via a host-supplied `IBrowser` to authenticate a user
- **Automatic Token Refresh** - Token attachment and refresh-on-401 handled by
  `Duende.IdentityModel.OidcClient`'s own `RefreshTokenDelegatingHandler`
- **Startup Validation** - Missing or invalid configuration throws immediately via
  `Zwedze.Aetherweave.Core`
- **Named Schemes** - Configure zero, one, or many independent login schemes

## Installation

```bash
dotnet add package Zwedze.Aetherweave.Security.Oidc.Native
```

## Configuration (`Aetherweave:Security:Oidc:Native`)

Each key under the section is a scheme name; the values are validated `PkceSchemeOptions`.

| Key | Notes |
|---|---|
| `Authority` | *Required.* The identity provider's URL |
| `ClientId` | *Required.* The registered client identifier |
| `ClientSecret` | Optional — only required if the client isn't public |
| `RedirectUri` | *Required.* Must match a redirect URI registered with the IDP, e.g. `http://127.0.0.1:7890/callback` |
| `Scope` | Defaults to `openid profile offline_access` — include `offline_access` so a refresh token is issued |

```json
{
  "Aetherweave": {
    "Security": {
      "Oidc": {
        "Native": {
          "desktop-app": {
            "Authority": "https://identity.example.com",
            "ClientId": "desktop-client",
            "RedirectUri": "http://127.0.0.1:7890/callback",
            "Scope": "openid profile offline_access orders.api"
          }
        }
      }
    }
  }
}
```

## Quick Start

### 1. Register named schemes

```csharp
services.AddAetherweaveNativeOidcAuthentication(configuration);

// Or with a custom section name
services.AddAetherweaveNativeOidcAuthentication(configuration, "MyApp:NativeLogin");
```

### 2. Register a keyed `IBrowser` per scheme

Launching a browser is inherently host-specific (system browser, embedded webview, etc.), so the host
app supplies it:

```csharp
services.AddKeyedSingleton<IBrowser>("desktop-app", (sp, _) => new MySystemBrowserLauncher());
```

### 3. Opt in per HttpClient

```csharp
services.AddAetherweaveHttpClient<IProfileServiceClient, ProfileServiceClient>(configuration, "ProfileService")
    .WithNativeOidcAuthentication("desktop-app");
```

The first call made through `ProfileService` triggers the interactive login automatically (via the
registered `IBrowser`) — no manual login step is required. From then on, token attachment and
refresh-on-401 are handled transparently by the `RefreshTokenDelegatingHandler` that Duende's
`OidcClient` returns from the login.

## Behavior notes

- **Login is per-HttpClient, not shared across clients.** Each `HttpClient` wired to a scheme performs
  its own independent interactive login and holds its own refresh token. If two typed clients reference
  the same scheme name, each triggers its own separate browser login the first time it's used. Sharing a
  single login across clients isn't supported, since many OIDC servers rotate/invalidate refresh tokens
  on each use — two independently-refreshing clients on "the same" login would eventually invalidate
  each other.
- **This is for native/desktop clients only** — this project has no ASP.NET Core dependency, so it
  cannot participate in cookie-based web app OIDC login. For an ASP.NET Core web app that needs
  delegated user tokens, use `Duende.AccessTokenManagement.OpenIdConnect` directly instead. For a
  browser-hosted Blazor WebAssembly app, use `Zwedze.Aetherweave.Security.Oidc`.

## Exceptions

| Exception | When |
|---|---|
| `AetherweaveAuthenticationException` | The interactive login fails, or succeeds without issuing a refresh token (check that `offline_access` is in `Scope`) |
| `PkceBrowserNotRegisteredException` | A login is attempted but no keyed `IBrowser` was registered for that scheme |

## API Reference

| Member | Signature |
|---|---|
| `AddAetherweaveNativeOidcAuthentication` | `(this IServiceCollection, IConfiguration, string sectionName = "Aetherweave:Security:Oidc:Native") → IServiceCollection` |
| `IHttpClientBuilder.WithNativeOidcAuthentication` | `(string schemeName) → IHttpClientBuilder` — attaches the interactive login handler to an `HttpClient` |
