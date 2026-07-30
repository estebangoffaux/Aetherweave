# Aetherweave.Grpc

Configuration-driven gRPC client registration for Aetherweave, built on `Grpc.Net.ClientFactory`.
This package is purely generic gRPC-client plumbing — it has no knowledge of any authentication
flow, exactly like `Zwedze.Aetherweave.Http` is for HTTP clients.

## Installation

```bash
dotnet add package Zwedze.Aetherweave.Grpc
```

## Quick Start

### 1. Reference your generated gRPC client

```csharp
// Generated from your .proto file, e.g. Greeter.GreeterClient
```

### 2. Configure in appsettings.json

```json
{
  "Aetherweave": {
    "GrpcClients": {
      "GreeterService": {
        "Address": "https://greeter.example.com",
        "Timeout": "00:00:30"
      }
    }
  }
}
```

### 3. Register in Program.cs

```csharp
services.AddAetherweaveGrpcClient<Greeter.GreeterClient>(configuration, "GreeterService");
```

### 4. Use in your application

```csharp
public sealed class GreeterService(Greeter.GreeterClient client)
{
    public async Task<string> GreetAsync(string name, CancellationToken ct)
    {
        var reply = await client.SayHelloAsync(new HelloRequest { Name = name }, cancellationToken: ct);
        return reply.Message;
    }
}
```

## Configuration Options

### GrpcClientOptions

| Property  | Type       | Default    | Required | Description                                  |
|-----------|------------|------------|----------|-----------------------------------------------|
| `Address` | `string`   | -          | ✅        | gRPC channel address (must be absolute URI)  |
| `Timeout` | `TimeSpan` | `00:00:30` | ❌        | Request timeout (must be > 0)                |

## Authentication

This package carries no auth code. Because `AddAetherweaveGrpcClient<TClient>` returns a plain
`IHttpClientBuilder` — the same type `Grpc.Net.ClientFactory`'s `AddGrpcClient<TClient>` returns —
the existing Aetherweave Security packages compose onto it directly:

```csharp
services.AddAetherweaveClientCredentialsAuthentication(configuration);

services.AddAetherweaveGrpcClient<Greeter.GreeterClient>(configuration, "GreeterService")
    .WithClientCredentialsAuthentication("greeter-api");
```

See `Zwedze.Aetherweave.Security.ClientCredentials` for backend-to-backend (OAuth2 client
credentials) authentication — the only auth case currently supported for gRPC clients.

## Error Handling

### ConfigurationNotFoundException

Thrown when the configuration section is not found:

```csharp
try
{
    services.AddAetherweaveGrpcClient<Greeter.GreeterClient>(
        configuration,
        "NonExistentClient");
}
catch (ConfigurationNotFoundException ex)
{
    // Configuration section 'Aetherweave:GrpcClients:NonExistentClient' not found.
    // Ensure your appsettings.json contains the required configuration.
}
```
