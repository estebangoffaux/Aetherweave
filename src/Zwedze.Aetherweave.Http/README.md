# Aetherweave.Http

Clean, type-safe HttpClient configuration with built-in profiling, content tracing, and error handling.

## Features

- ⚙️ **Configuration-Based Setup** - Configure HttpClients from appsettings.json with IOptions validation
- 📊 **Built-in Profiling** - Automatic request timing and performance tracking
- 🔍 **Content Tracing** - Log response content for debugging
- 🛡️ **Error Handling** - Custom error handlers for failed HTTP requests
- 🔗 **Chainable API** - Fluent builder pattern for adding handlers
- ✅ **Startup Validation** - Configuration validated at application startup
- 🎯 **Type-Safe** - Strongly-typed clients with dependency injection

## Installation

```bash
dotnet add package Zwedze.Aetherweave.Http
```

## Quick Start

### 1. Define Your HTTP Client Interface

```csharp
public interface IOrderServiceClient
{
    Task<Order> GetOrderAsync(int orderId, CancellationToken ct);
    Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct);
}

public class OrderServiceClient(HttpClient httpClient) : IOrderServiceClient
{
    public async Task<Order> GetOrderAsync(int orderId, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"/api/orders/{orderId}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Order>(ct) 
            ?? throw new InvalidOperationException("Order not found");
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync("/api/orders", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Order>(ct) 
            ?? throw new InvalidOperationException("Failed to create order");
    }
}
```

### 2. Configure in appsettings.json

```json
{
  "Aetherweave": {
    "HttpClients": {
      "OrderService": {
        "BaseAddress": "https://api.orders.example.com",
        "Timeout": "00:00:30",
        "EnableProfiling": true,
        "EnableContentTracing": false,
        "MaxContentLogSize": 10000
      }
    }
  }
}
```

### 3. Register in Program.cs

```csharp
services.AddAetherweaveHttpClient<IOrderServiceClient, OrderServiceClient>(
    configuration,
    "OrderService");
```

### 4. Use in Your Application

```csharp
public class OrderController(IOrderServiceClient orderClient) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id, CancellationToken ct)
    {
        var order = await orderClient.GetOrderAsync(id, ct);
        return Ok(order);
    }
}
```

## Configuration Options

### HttpClientOptions

| Property               | Type       | Default    | Required | Description                                         |
|------------------------|------------|------------|----------|-----------------------------------------------------|
| `BaseAddress`          | `string`   | -          | ✅        | Base URL for the HTTP client (must be absolute URI) |
| `Timeout`              | `TimeSpan` | `00:00:30` | ❌        | Request timeout (must be > 0)                       |
| `EnableProfiling`      | `bool`     | `false`    | ❌        | Enable request timing and performance logging       |
| `EnableContentTracing` | `bool`     | `false`    | ❌        | Enable response content logging                     |
| `MaxContentLogSize`    | `int`      | `10000`    | ❌        | Maximum bytes to log (content truncated if larger)  |

### Multiple Clients Configuration

```json
{
  "Aetherweave": {
    "HttpClients": {
      "OrderService": {
        "BaseAddress": "https://api.orders.example.com",
        "Timeout": "00:00:30",
        "EnableProfiling": true,
        "EnableContentTracing": false
      },
      "PaymentService": {
        "BaseAddress": "https://api.payments.example.com",
        "Timeout": "00:01:00",
        "EnableProfiling": false,
        "EnableContentTracing": true,
        "MaxContentLogSize": 5000
      },
      "InventoryService": {
        "BaseAddress": "https://api.inventory.example.com",
        "Timeout": "00:00:15",
        "EnableProfiling": true,
        "EnableContentTracing": true
      }
    }
  }
}
```

```csharp
// Register all clients
services.AddAetherweaveHttpClient<IOrderServiceClient, OrderServiceClient>(
    configuration, "OrderService");
    
services.AddAetherweaveHttpClient<IPaymentServiceClient, PaymentServiceClient>(
    configuration, "PaymentService");
    
services.AddAetherweaveHttpClient<IInventoryServiceClient, InventoryServiceClient>(
    configuration, "InventoryService");
```

## Built-in Handlers

### Profiling Handler

Automatically logs request timing when `EnableProfiling` is `true`:

```
[2025-12-20 10:30:45] HTTP GET https://api.orders.example.com/api/orders/123 completed in 245ms with status 200
```

**Configuration:**

```json
{
  "OrderService": {
    "BaseAddress": "https://api.orders.example.com",
    "EnableProfiling": true
  }
}
```

### Content Tracing Handler

Logs response content when `EnableContentTracing` is `true`:

```
[2025-12-20 10:30:45] HTTP GET https://api.orders.example.com/api/orders/123 returned 200 (1234 bytes): {"orderId":123,"total":99.99,...}
```

**Configuration:**

```json
{
  "OrderService": {
    "BaseAddress": "https://api.orders.example.com",
    "EnableContentTracing": true,
    "MaxContentLogSize": 5000
  }
}
```

**Security Warning:** ⚠️ Never enable `EnableContentTracing` in production with sensitive data (passwords, credit cards,
etc.)!

## Advanced Usage

### Adding Custom Handlers

```csharp
services.AddAetherweaveHttpClient<IOrderServiceClient, OrderServiceClient>(
        configuration, 
        "OrderService")
    .AddAetherweaveHandler<AuthenticationHandler>()
    .AddAetherweaveHandler<RetryPolicyHandler>();
```

**Custom handler example:**

```csharp
public class AuthenticationHandler(ITokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        return await base.SendAsync(request, cancellationToken);
    }
}

// Register
services.AddAetherweaveHttpClient<IOrderServiceClient, OrderServiceClient>(
        configuration, 
        "OrderService")
    .AddAetherweaveHandler<AuthenticationHandler>();
```

### Custom Error Handling

```csharp
public class OrderServiceErrorHandler(ILogger<OrderServiceErrorHandler> logger) : IHttpErrorHandler
{
    public async Task HandleError(
        HttpRequestMessage request,
        HttpResponseMessage response,
        HttpStatusCode statusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        
        logger.LogError(
            "Order service request to {Uri} failed with {StatusCode}: {Content}",
            request.RequestUri,
            statusCode,
            content);

        throw statusCode switch
        {
            HttpStatusCode.NotFound => new OrderNotFoundException(content),
            HttpStatusCode.BadRequest => new InvalidOrderException(content),
            HttpStatusCode.Unauthorized => new UnauthorizedException(),
            _ => new OrderServiceException($"Request failed with status {statusCode}")
        };
    }
}

// Register
services.AddAetherweaveHttpClient<IOrderServiceClient, OrderServiceClient>(
        configuration, 
        "OrderService")
    .AddAetherweaveErrorHandler<OrderServiceErrorHandler>();
```

### Combining Multiple Handlers

```csharp
services.AddAetherweaveHttpClient<IOrderServiceClient, OrderServiceClient>(
        configuration, 
        "OrderService")
    .AddAetherweaveHandler<AuthenticationHandler>()
    .AddAetherweaveHandler<RetryPolicyHandler>()
    .AddAetherweaveErrorHandler<OrderServiceErrorHandler>();
```

**Handler execution order:**

1. ProfilingHandler (starts timer) - built-in, outermost
2. ContentTracingHandler (logs response) - built-in
3. AuthenticationHandler (adds token)
4. RetryPolicyHandler (retries on failure)
5. HttpErrorResponseHandler (custom error handling)
6. → Actual HTTP request →
7. HttpErrorResponseHandler (processes errors)
8. RetryPolicyHandler (retry if needed)
9. AuthenticationHandler
10. ContentTracingHandler (logs response content)
11. ProfilingHandler (logs timing)

### Environment-Specific Configuration

**appsettings.Development.json:**

```json
{
  "Aetherweave": {
    "HttpClients": {
      "OrderService": {
        "BaseAddress": "https://dev-api.orders.example.com",
        "Timeout": "00:05:00",
        "EnableProfiling": true,
        "EnableContentTracing": true,
        "MaxContentLogSize": 50000
      }
    }
  }
}
```

**appsettings.Production.json:**

```json
{
  "Aetherweave": {
    "HttpClients": {
      "OrderService": {
        "BaseAddress": "https://api.orders.example.com",
        "Timeout": "00:00:30",
        "EnableProfiling": false,
        "EnableContentTracing": false
      }
    }
  }
}
```

### Using with Polly for Resilience

```csharp
services.AddAetherweaveHttpClient<IOrderServiceClient, OrderServiceClient>(
        configuration, 
        "OrderService")
    .AddPolicyHandler(Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(r => !r.IsSuccessStatusCode)
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)));
```

## Integration with Aetherweave.Application

### Command Handler with HTTP Client

```csharp
public class CreateOrderHandler(
    IOrderServiceClient orderServiceClient,
    IUnitOfWorkFactory uowFactory,
    ApplicationDbContext dbContext) : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<ResponseWrapper<Guid>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        await using var uow = uowFactory.CreateTransactional();
        
        try
        {
            // Create order via external API
            var externalOrder = await orderServiceClient.CreateOrderAsync(
                new CreateOrderRequest(request.Items),
                cancellationToken);
            
            // Save locally
            var order = new Order(
                Id<Order>.From(externalOrder.Id),
                Code<Order>.From(externalOrder.OrderNumber));
                
            dbContext.Orders.Add(order);
            await uow.SaveChanges(cancellationToken);
            await uow.Commit(cancellationToken);
            
            return ResponseWrapper.Ok(order.Id);
        }
        catch (OrderServiceException ex)
        {
            return ResponseWrapper.Fail<Guid>(
                ErrorFactory.Create("EXTERNAL_SERVICE_ERROR", ex.Message));
        }
    }
}
```

## Best Practices

### ✅ DO

1. **Use interfaces for HTTP clients:**
   ```csharp
   // Good
   public interface IOrderServiceClient { ... }
   public class OrderServiceClient : IOrderServiceClient { ... }
   
   // Bad
   public class OrderServiceClient { ... }  // No interface
   ```

2. **Configure separate clients for different services:**
   ```csharp
   services.AddAetherweaveHttpClient<IOrderServiceClient, OrderServiceClient>(...);
   services.AddAetherweaveHttpClient<IPaymentServiceClient, PaymentServiceClient>(...);
   ```

3. **Use configuration for environment-specific settings:**
   ```json
   // appsettings.Development.json
   { "EnableContentTracing": true }
   
   // appsettings.Production.json
   { "EnableContentTracing": false }
   ```

4. **Set appropriate timeouts:**
   ```json
   {
     "Timeout": "00:00:30"  // 30 seconds for quick APIs
     "Timeout": "00:05:00"  // 5 minutes for long-running operations
   }
   ```

5. **Use custom error handlers for domain-specific errors:**
   ```csharp
   .AddAetherweaveErrorHandler<OrderServiceErrorHandler>()
   ```

### ❌ DON'T

1. **Don't hardcode URLs in client implementations:**
   ```csharp
   // Bad
   var response = await httpClient.GetAsync("https://hardcoded-url.com/api");
   
   // Good - use BaseAddress from config
   var response = await httpClient.GetAsync("/api/orders");
   ```

2. **Don't enable content tracing with sensitive data:**
   ```json
   // BAD in production!
   {
     "PaymentService": {
       "EnableContentTracing": true  // Could log credit cards!
     }
   }
   ```

3. **Don't use the same client name for different services:**
   ```csharp
   // Bad - both use "ApiClient"
   services.AddAetherweaveHttpClient<IOrderClient, OrderClient>(config, "ApiClient");
   services.AddAetherweaveHttpClient<IPaymentClient, PaymentClient>(config, "ApiClient");
   ```

4. **Remember to validate configuration at startup:**
   ```csharp
   // Configuration automatically validated with .ValidateOnStart()
   // Will fail fast if BaseAddress is missing or invalid
   ```

## Error Handling

### ConfigurationNotFoundException

Thrown when the configuration section is not found:

```csharp
try
{
    services.AddAetherweaveHttpClient<IOrderClient, OrderClient>(
        configuration, 
        "NonExistentClient");
}
catch (ConfigurationNotFoundException ex)
{
    // Configuration section 'Aetherweave:HttpClients:NonExistentClient' not found.
    // Ensure your appsettings.json contains the required configuration.
}
```

### Validation Errors

Configuration validated at startup with detailed error messages:

```
Unhandled exception. Microsoft.Extensions.Options.OptionsValidationException: 
DataAnnotation validation failed for 'HttpClientOptions' members: 'Timeout' 
with the error: 'Timeout must be greater than zero'.
```

## Performance Considerations

### Profiling Overhead

When `EnableProfiling` is `true`:

- Minimal overhead (~1-2ms per request)
- Only measures elapsed time
- Safe for production use

### Content Tracing Overhead

When `EnableContentTracing` is `true`:

- Significant overhead (reads entire response into memory)
- Doubles memory usage for response
- **Not recommended for production**
- Use only for debugging/development

### Handler Order Optimization

Handlers execute in order of registration:

```csharp
// Optimal order for performance
services.AddAetherweaveHttpClient<IClient, Client>(config, "Client")
    .AddAetherweaveHandler<CacheHandler>()        // Check cache first
    .AddAetherweaveHandler<AuthenticationHandler>() // Then auth
    .AddAetherweaveHandler<RetryPolicyHandler>();   // Retry last
```

## Migration from HttpClientFactory

**Before (raw HttpClientFactory):**

```csharp
services.AddHttpClient("OrderService", client =>
{
    client.BaseAddress = new Uri("https://api.orders.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Usage
public class OrderService(IHttpClientFactory httpClientFactory)
{
    public async Task<Order> GetOrderAsync(int id)
    {
        var client = httpClientFactory.CreateClient("OrderService");
        var response = await client.GetAsync($"/api/orders/{id}");
        // ...
    }
}
```

**After (Aetherweave):**

```csharp
// appsettings.json
{
  "Aetherweave": {
    "HttpClients": {
      "OrderService": {
        "BaseAddress": "https://api.orders.com",
        "Timeout": "00:00:30"
      }
    }
  }
}

// Program.cs
services.AddAetherweaveHttpClient<IOrderServiceClient, OrderServiceClient>(
    configuration,
    "OrderService");

// Usage
public class OrderService(IOrderServiceClient orderClient)
{
    public async Task<Order> GetOrderAsync(int id)
    {
        return await orderClient.GetOrderAsync(id);
    }
}
```
