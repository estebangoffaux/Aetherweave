# Aetherweave.Security.Jwt

Configuration-driven JWT Bearer authentication for APIs, built on `Zwedze.Aetherweave.Core`'s validated options loading.

## Features

- **One-Line Registration** - Configure `AddJwtBearer` from `appsettings.json` instead of hand-wiring `JwtBearerOptions`
- **Startup Validation** - Missing or invalid configuration throws immediately, not on first request
- **Type-Safe Options** - `AetherweaveJwtBearerOptions` validated via data annotations

## Installation

```bash
dotnet add package Zwedze.Aetherweave.Security.Jwt
```

## Configuration (`Aetherweave:Security:JwtBearer`)

| Key | Notes |
|---|---|
| `Authority` | *Required.* The token issuer, e.g. your identity provider's URL |
| `Audience` | *Required.* The expected `aud` claim |
| `RequireHttpsMetadata` | *Required.* Set `false` only for local development |

```json
{
  "Aetherweave": {
    "Security": {
      "JwtBearer": {
        "Authority": "https://identity.example.com",
        "Audience": "orders-api",
        "RequireHttpsMetadata": true
      }
    }
  }
}
```

## Quick Start

```csharp
services.AddAetherweaveJwtBearerAuthentication(configuration);

// Or with a custom section name
services.AddAetherweaveJwtBearerAuthentication(configuration, "MyApp:Jwt");
```

This reads and validates `AetherweaveJwtBearerOptions` from configuration, then registers `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` with `Authority`, `Audience`, and `RequireHttpsMetadata` set accordingly.

Remember to also call `app.UseAuthentication()` and `app.UseAuthorization()` in your pipeline, and add `[Authorize]` to the endpoints you want protected — this package only wires up the JWT Bearer handler.

## API Reference

| Member | Signature |
|---|---|
| `AddAetherweaveJwtBearerAuthentication` | `(this IServiceCollection, IConfiguration, string sectionName = "Aetherweave:Security:JwtBearer") → IServiceCollection` |
