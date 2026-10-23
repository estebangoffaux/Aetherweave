# Aetherweave

<img src="Icon.png" alt="Aetherweave" width="50" />

A pragmatic framework for building clean, maintainable .NET applications using Domain-Driven Design and clean
architecture patterns.

## Philosophy

Modern software development demands clarity and maintainability. Aetherweave provides the building blocks you need
without imposing unnecessary ceremony or complexity. Each package solves a specific problem and integrates seamlessly
with the others.

## Packages

### Core Building Blocks

#### [Aetherweave.SharedKernel](src/Zwedze.Aetherweave.SharedKernel/README.md)

The foundation. Type-safe value objects (`Id<T>`, `Code<T>`), aggregate roots, domain events, and business exceptions.
If you're doing DDD, start here.

**Key Features:**

- Type-safe entity identifiers that prevent mixing IDs across entities
- Aggregate root base class with built-in domain event support
- Business exception infrastructure for domain errors
- Value object primitives for natural keys and codes

#### [Aetherweave.Application](src/Zwedze.Aetherweave.Application/README.md)

CQRS interfaces, domain event dispatching, and result patterns. The application layer that orchestrates your domain
logic.

**Key Features:**

- Command and Query handler abstractions
- Fluent domain event dispatcher with multi-handler support
- ResponseWrapper for type-safe success/failure responses
- Error factory for consistent error handling

### Infrastructure & Integration

#### [Aetherweave.Data.Relational](src/Zwedze.Aetherweave.Data.Relational/Zwedze.Aetherweave.Data.Relational.csproj)

Clean EF Core wrapper with Unit of Work pattern. Transactional integrity without the boilerplate.

**Key Features:**

- Transactional and non-transactional unit of work support
- Configuration-based DbContext setup from appsettings
- Auto-starting transactions (no manual BeginTransaction needed)
- Built-in health checks for production monitoring

#### [Aetherweave.Http](src/Zwedze.Aetherweave.Http/README.md)

Type-safe HttpClient configuration with built-in profiling, content tracing, and error handling.

**Key Features:**

- Configuration-based HttpClient setup from appsettings
- Automatic request profiling and performance tracking
- Content tracing for debugging
- Chainable handler API and custom error handling

#### [Aetherweave.IdentityGenerators](src/Zwedze.Aetherweave.IdentityGenerators/README.md)

Distributed ID generation using Twitter's Snowflake algorithm. Generate unique, sortable identifiers across multiple
instances.

**Key Features:**

- 64-bit distributed unique identifiers
- Direct support for `Id<T>` and `Code<T>` from SharedKernel
- Multi-instance support with machine name or IP-based identification
- Custom epoch configuration for maximum lifespan

## Getting Started

### 1. Install Core Packages

```bash
# Domain building blocks
dotnet add package Zwedze.Aetherweave.SharedKernel

# Application layer patterns
dotnet add package Zwedze.Aetherweave.Application
```

### 2. Add Infrastructure as Needed

```bash
# Database access
dotnet add package Zwedze.Aetherweave.Data.Relational

# HTTP clients
dotnet add package Zwedze.Aetherweave.Http

# ID generation
dotnet add package Zwedze.Aetherweave.IdentityGenerators
```

### 3. Define Your Domain

```csharp
// Aggregate root: internal Id<T> + external Code<T>, private constructor, Create/Reconstitute factories
public sealed class Order : AggregateRoot<Order>
{
    private Order(Id<Order> id, Code<Order> code, Id<Customer> customerId, OrderStatus status)
        : base(id, code)
    {
        CustomerId = customerId;
        Status = status;
    }

    public Id<Customer> CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }

    public static Order Create(Id<Order> id, Code<Order> code, Id<Customer> customerId)
    {
        var order = new Order(id, code, customerId, OrderStatus.Draft);
        order.RaiseDomainEvent(new OrderCreatedEvent(order.Id, customerId));
        return order;
    }

    public static Order Reconstitute(Id<Order> id, Code<Order> code, Id<Customer> customerId, OrderStatus status)
    {
        return new Order(id, code, customerId, status); // never raises events
    }

    public void Submit()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new OrderNotEditableException(Id, Status);
        }

        Status = OrderStatus.Submitted;
        RaiseDomainEvent(new OrderSubmittedEvent(Id));
    }
}

// Every error code is declared once, in the domain
public static class BusinessRuleErrorCodes
{
    public static readonly ErrorCode OrderNotFound = ErrorCode.New("ORDER_NOT-FOUND");
    public static readonly ErrorCode OrderNotEditable = ErrorCode.New("ORDER_NOT-EDITABLE");
    public static readonly ErrorCode CustomerNotFound = ErrorCode.New("CUSTOMER_NOT-FOUND");
    public static readonly ErrorCode PaymentRefused = ErrorCode.New("PAYMENT_REFUSED");
}

public sealed class OrderNotEditableException(Id<Order> orderId, OrderStatus status)
    : BusinessException(BusinessRuleErrorCodes.OrderNotEditable, $"Order {orderId} is {status} and can no longer be modified.")
{
    public Id<Order> OrderId { get; } = orderId;
}
```

### 4. Implement Application Layer

```csharp
// The command references other aggregates by their external Code<T>
public sealed record CreateOrderCommand(Code<Customer> CustomerCode);

// Command handler: returns a ResponseWrapper, never lets a business failure escape as an exception
internal sealed class CreateOrderHandler(
    IIdentityGenerator identityGenerator,
    ICustomerRepository customerRepository,
    IOrderRepository orderRepository,
    IUnitOfWorkFactory uowFactory,
    IDomainEventDispatcher eventDispatcher
) : ICommandHandler<CreateOrderCommand, Code<Order>>
{
    public async Task<ResponseWrapper<Code<Order>>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var customer = await customerRepository.GetByCodeAsync(request.CustomerCode, ct);
        if (customer is null)
        {
            return ResponseWrapper.Fail<Code<Order>>(
                ErrorFactory.Create(BusinessRuleErrorCodes.CustomerNotFound, $"Customer {request.CustomerCode} not found."));
        }

        var order = Order.Create(
            identityGenerator.CreateId<Order>(),   // internal Snowflake Id<T>
            identityGenerator.CreateCode<Order>(), // external UUID v7 Code<T>
            customer.Id);

        await using var uow = uowFactory.CreateTransactional();
        await orderRepository.AddAsync(order, ct);
        await uow.SaveChanges(ct);
        await uow.Commit(ct);

        // After commit; no ct: event handlers get the application lifetime token
        await eventDispatcher.Dispatch(order);

        return ResponseWrapper.Ok(order.Code); // external contracts carry Code<T>, never Id<T>
    }
}
```

### 5. Configure Services

```csharp
var builder = WebApplication.CreateBuilder(args);

// Identity generation
builder.Services.AddAetherweaveGenerators(options =>
{
    options.InstanceIdType = InstanceIdType.MachineName;
    options.EpochStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
});

// Data access
builder.Services.AddAetherweaveData<ApplicationDbContext>(
    builder.Configuration,
    (dbBuilder, options) => dbBuilder.UseNpgsql(
        builder.Configuration.GetConnectionString(options.ConnectionStringName)));

// Use case handlers
builder.Services.AddScoped<ICommandHandler<CreateOrderCommand, Code<Order>>, CreateOrderHandler>();

// Domain events: each handler is registered in DI and mapped to its event
builder.Services.AddScoped<OrderSubmittedEmailHandler>();
builder.Services.AddScoped<OrderSubmittedAnalyticsHandler>();
builder.Services.AddAetherweaveDomainEventDispatcher(registry =>
{
    registry.Configure<OrderSubmittedEvent>()
        .AddHandler<OrderSubmittedEmailHandler>()
        .AddHandler<OrderSubmittedAnalyticsHandler>();
});

// HTTP clients
builder.Services.AddAetherweaveHttpClient<IPaymentServiceClient, PaymentServiceClient>(
    builder.Configuration,
    "PaymentService");

var app = builder.Build();
```

## Design Principles

**Type Safety First**  
Leverage the compiler to catch errors early. `Id<Order>` can't be assigned to `Id<Customer>`. Period.

**Explicitness Over Magic**  
Configuration is visible and testable. Transactions require explicit commits. No surprises.

**Composability**  
Small, focused packages that work together. Use what you need, ignore the rest.

**Production Ready**  
Built-in health checks, profiling, error handling, and monitoring. Because uptime matters.

## Real-World Example

An order submission with payment, from the HTTP endpoint down to the domain events, using SharedKernel, Application,
Data.Relational and Http together:

```csharp
// Application: command + handler
public sealed record SubmitOrderCommand(Code<Order> OrderCode);

internal sealed class SubmitOrderHandler(
    IOrderRepository orderRepository,
    IPaymentServiceClient paymentClient,
    IUnitOfWorkFactory uowFactory,
    IDomainEventDispatcher eventDispatcher,
    ILogger<SubmitOrderHandler> logger
) : ICommandHandler<SubmitOrderCommand, Code<Order>>
{
    public async Task<ResponseWrapper<Code<Order>>> Handle(SubmitOrderCommand request, CancellationToken ct)
    {
        // 1. Load the aggregate by its external code
        var order = await orderRepository.GetByCodeAsync(request.OrderCode, ct);
        if (order is null)
        {
            return ResponseWrapper.Fail<Code<Order>>(
                ErrorFactory.Create(BusinessRuleErrorCodes.OrderNotFound, $"Order {request.OrderCode} not found."));
        }

        // 2. Apply the business rule; a violation becomes a Failure, not an exception
        try
        {
            order.Submit();
        }
        catch (BusinessException ex)
        {
            return ResponseWrapper.Fail<Code<Order>>(ErrorFactory.Create(ex));
        }

        // 3. Persist inside an explicit transaction
        await using var uow = uowFactory.CreateTransactional();
        await orderRepository.UpdateStatusAsync(order, ct);
        await uow.SaveChanges(ct);

        // 4. External HTTP call (Zwedze.Aetherweave.Http): send the external Code, never the internal Id
        var payment = await paymentClient.ProcessPaymentAsync(new PaymentRequest((string)order.Code), ct);
        if (!payment.IsSuccess)
        {
            await uow.Rollback(ct);
            logger.LogWarning("Payment refused for order {OrderCode}", order.Code);
            return ResponseWrapper.Fail<Code<Order>>(
                ErrorFactory.Create(BusinessRuleErrorCodes.PaymentRefused, $"Payment for order {order.Code} was refused."));
        }

        await uow.Commit(ct);

        // 5. Side effects only after commit; no ct: event handlers get the application lifetime token
        await eventDispatcher.Dispatch(order);

        logger.LogInformation("Order {OrderCode} submitted", order.Code);
        return ResponseWrapper.Ok(order.Code);
    }
}

// Host: thin controller, maps the ResponseWrapper to HTTP
[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController(ICommandHandler<SubmitOrderCommand, Code<Order>> submitOrderHandler) : ControllerBase
{
    [HttpPost("{code}/submit")]
    public async Task<IActionResult> Submit(string code, CancellationToken ct)
    {
        var result = await submitOrderHandler.Handle(new SubmitOrderCommand(Code<Order>.From(code)), ct);

        return result switch
        {
            ResponseWrapper<Code<Order>>.Success s => Ok((string)s.Value),
            ResponseWrapper<Code<Order>>.Failure f => BadRequest(f.Error),
            _ => StatusCode(500)
        };
    }
}
```

## When to Use Aetherweave

**Good Fit:**

- Domain-rich applications where business logic complexity justifies DDD
- Distributed systems needing unique ID generation across instances
- Teams valuing type safety and compile-time guarantees
- Applications requiring clear separation of concerns
- Projects where maintainability is a priority

**Might Be Overkill:**

- Simple CRUD applications with minimal business logic
- Prototypes or throwaway code
- Very small teams unfamiliar with DDD concepts
- Applications with strict performance constraints at all costs
