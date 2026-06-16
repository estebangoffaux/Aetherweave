# Aetherweave

<img src="Icon.png" alt="Aetherweave" width="50" />

A pragmatic framework for building clean, maintainable .NET applications using Domain-Driven Design and functional
programming patterns.

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

#### [Aetherweave.FunctionalProgramming](src/Zwedze.Aetherweave.FunctionalProgramming/README.md)

Railway-oriented programming for .NET. Chain operations that can fail elegantly without drowning in try-catch blocks.

**Key Features:**

- Result&lt;T&gt; for type-safe error handling
- Map, Bind, and Match for composable operations
- Async-first with comprehensive async support
- Integration with CQRS patterns

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

# Functional error handling
dotnet add package Zwedze.Aetherweave.FunctionalProgramming
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
// Domain entity with type-safe IDs
public class Order : AggregateRoot<Order>
{
    public Order(Id<Order> id, Code<Order> orderNumber) : base(id, orderNumber)
    {
        Status = OrderStatus.Draft;
    }

    public decimal Total { get; private set; }
    public OrderStatus Status { get; private set; }

    public void Submit()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOrderStateException(Status, OrderStatus.Draft);

        Status = OrderStatus.Submitted;
        RaiseDomainEvent(new OrderSubmittedEvent(Id, Total));
    }
}
```

### 4. Implement Application Layer

```csharp
// Command handler with transactional UoW
public class CreateOrderHandler(
    IIdentityGenerator identityGenerator,
    IOrderRepository orderRepository,
    IUnitOfWorkFactory uowFactory,
    IDomainEventDispatcher eventDispatcher) 
    : ICommandHandler<CreateOrderCommand, Id<Order>>
{
    public async Task<ResponseWrapper<Id<Order>>> Handle(
        CreateOrderCommand request,
        CancellationToken ct)
    {
        return await Result<CreateOrderCommand>.Success(request)
            .Bind(ValidateCommand)
            .BindAsync(async cmd =>
            {
                await using var uow = uowFactory.CreateTransactional();
                
                var order = Order.Create(identityGenerator, cmd.CustomerId, cmd.Items);
                await orderRepository.AddAsync(order, ct);
                
                await uow.SaveChanges(ct);
                await uow.Commit(ct);
                
                // Dispatch events after successful commit
                await eventDispatcher.DispatchAsync(order, ct);
                
                return Result<Order>.Success(order);
            })
            .Map(order => order.Id)
            .Match(
                onSuccess: id => ResponseWrapper<Id<Order>>.Ok(id),
                onFailure: error => ResponseWrapper<Id<Order>>.Fail(
                    ErrorFactory.Create(error.Code, error.Message))
            );
    }

    private Result<CreateOrderCommand> ValidateCommand(CreateOrderCommand cmd)
    {
        // Validation logic
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

// Domain events
builder.Services.AddDomainEventDispatcher(registry =>
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

A complete order processing workflow demonstrating multiple packages working together:

```csharp
public class OrderService(
    IIdentityGenerator identityGenerator,
    IOrderRepository orderRepository,
    IPaymentServiceClient paymentClient,
    IUnitOfWorkFactory uowFactory,
    IDomainEventDispatcher eventDispatcher,
    ILogger<OrderService> logger)
{
    public async Task<Result<Id<Order>>> ProcessOrder(
        CreateOrderCommand command,
        CancellationToken ct)
    {
        // Railway-oriented programming for clean error handling
        return await Result<CreateOrderCommand>.Success(command)
            .Bind(ValidateOrder)
            .BindAsync(CheckInventory)
            .BindAsync(async cmd =>
            {
                // Transactional unit of work
                await using var uow = uowFactory.CreateTransactional();
                
                // Type-safe ID generation
                var order = Order.Create(
                    identityGenerator,
                    cmd.CustomerId,
                    cmd.Items);
                
                await orderRepository.AddAsync(order, ct);
                await uow.SaveChanges(ct);
                
                // External HTTP call
                var paymentResult = await paymentClient.ProcessPaymentAsync(
                    new PaymentRequest(order.Id, order.Total),
                    ct);
                
                if (!paymentResult.IsSuccess)
                {
                    await uow.Rollback(ct);
                    return Result<Order>.Fail(
                        new Error("PAYMENT_FAILED", "Payment processing failed"));
                }
                
                order.MarkAsPaid();
                await uow.SaveChanges(ct);
                await uow.Commit(ct);
                
                // Domain events after successful commit
                await eventDispatcher.DispatchAsync(order, ct);
                
                return Result<Order>.Success(order);
            })
            .Map(order => order.Id)
            .OnSuccess(id => logger.LogInformation("Order {OrderId} processed", id))
            .OnFailure(error => logger.LogError("Order processing failed: {Error}", error.Message));
    }

    private Result<CreateOrderCommand> ValidateOrder(CreateOrderCommand cmd)
    {
        if (!cmd.Items.Any())
            return Result<CreateOrderCommand>.Fail(
                new Error("EMPTY_ORDER", "Order must contain items"));
        
        return Result<CreateOrderCommand>.Success(cmd);
    }

    private async Task<Result<CreateOrderCommand>> CheckInventory(
        CreateOrderCommand cmd)
    {
        // Inventory check logic
        return Result<CreateOrderCommand>.Success(cmd);
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
