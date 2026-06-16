# Aetherweave.Application

Clean architecture application layer providing CQRS, domain events, and result patterns for building maintainable
applications.

## Features

- ✨ **Domain Event Dispatcher** - Fluent API for registering and dispatching domain events
- 🎯 **CQRS Interfaces** - Command and Query handler abstractions
- 🔄 **Result Pattern** - Type-safe success/failure responses with discriminated unions
- 🛡️ **Error Handling** - Standardized error creation from exceptions
- 🧩 **DI Integration** - First-class support for dependency injection

## Installation

```bash
dotnet add package Zwedze.Aetherweave.Application
```

## Quick Start

### 1. Domain Events

**Define your domain events:**

```csharp
public record OrderCreatedEvent(Guid OrderId, decimal Amount) : DomainEvent;
public record OrderShippedEvent(Guid OrderId, string TrackingNumber) : DomainEvent;
```

**Create handlers:**

```csharp
public class OrderCreatedEmailHandler(IEmailService emailService) : IDomainEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct)
    {
        await emailService.SendOrderConfirmation(@event.OrderId, ct);
    }
}

public class OrderCreatedSmsHandler(ISmsService smsService) : IDomainEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct)
    {
        await smsService.SendOrderConfirmation(@event.OrderId, ct);
    }
}
```

**Register with beautiful fluent API:**

```csharp
services.AddAetherweaveDomainEventDispatcher(registry =>
{
    // Multiple handlers for the same event
    registry.Configure<OrderCreatedEvent>()
        .AddHandler<OrderCreatedEmailHandler>()
        .AddHandler<OrderCreatedSmsHandler>()
        .AddHandler<OrderCreatedAnalyticsHandler>();

    // Chain to other events
    registry.Configure<OrderShippedEvent>()
        .AddHandler<OrderShippedNotificationHandler>()
        .AddHandler<OrderShippedTrackingHandler>();

    // Shorthand for single handlers
    registry.AddHandler<PaymentReceivedEvent, PaymentReceivedHandler>();
});
```

**Dispatch events from aggregates:**

```csharp
public class OrderService(
    IUnitOfWorkFactory uowFactory,
    IDomainEventDispatcher eventDispatcher)
{
    public async Task CreateOrder(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        await using var uow = uowFactory.CreateTransactional();
        
        var order = new Order(command.Items);
        // order.RaiseDomainEvent(new OrderCreatedEvent(...));
        
        await uow.SaveChanges(cancellationToken);
        await uow.Commit(cancellationToken);
        
        // Dispatch events after successful commit
        await eventDispatcher.DispatchAsync(order, cancellationToken);
    }
}
```

### 2. CQRS - Commands

**Define command and handler:**

```csharp
public record CreateOrderCommand(Guid CustomerId, List<OrderItem> Items);

public class CreateOrderHandler(
    IOrderRepository orderRepository,
    IUnitOfWorkFactory uowFactory) : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<ResponseWrapper<Guid>> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            await using var uow = uowFactory.CreateTransactional();
            
            var order = new Order(request.CustomerId, request.Items);
            await orderRepository.AddAsync(order, cancellationToken);
            
            await uow.SaveChanges(cancellationToken);
            await uow.Commit(cancellationToken);
            
            return ResponseWrapper.Ok(order.Id);
        }
        catch (BusinessException ex)
        {
            var error = ErrorFactory.Create(ex);
            return ResponseWrapper.Fail<Guid>(error);
        }
    }
}
```

**Use in your API:**

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController(ICommandHandler<CreateOrderCommand, Guid> createOrderHandler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
    {
        var result = await createOrderHandler.Handle(command);

        return result switch
        {
            ResponseWrapper<Guid>.Success s => Ok(new { orderId = s.Value }),
            ResponseWrapper<Guid>.Failure f => BadRequest(f.Error),
            _ => StatusCode(500)
        };
    }
}
```

### 3. CQRS - Queries

**Define query and handler:**

```csharp
public record GetOrderByIdQuery(Guid OrderId);

public class GetOrderByIdHandler(IOrderRepository orderRepository) : IQueryHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<ResponseWrapper<OrderDto>> Handle(
        GetOrderByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            var error = ErrorFactory.Create("ORDER_NOT_FOUND", "Order not found");
            return ResponseWrapper.Fail<OrderDto>(error);
        }

        var dto = MapToDto(order);
        return ResponseWrapper.Ok(dto);
    }
    
    private static OrderDto MapToDto(Order order) => new(order.Id, order.Total);
}
```

**Parameterless query:**

```csharp
public class GetAllOrdersHandler(IOrderRepository orderRepository) : IQueryHandler<IReadOnlyCollection<OrderDto>>
{
    public async Task<ResponseWrapper<IReadOnlyCollection<OrderDto>>> Handle(CancellationToken cancellationToken)
    {
        var orders = await orderRepository.GetAllAsync(cancellationToken);
        var dtos = orders.Select(o => new OrderDto(o.Id, o.Total)).ToList();
        return ResponseWrapper.Ok<IReadOnlyCollection<OrderDto>>(dtos);
    }
}
```

## Advanced Usage

### Domain Event Dispatcher - Error Handling

The dispatcher collects all exceptions and throws an `AggregateException` if any handler fails. This ensures all
handlers run (eventual consistency):

> it is highly recommended to always wrap the dispatch in a try/catch block to avoid missing exceptions.
```csharp
try
{
    await eventDispatcher.DispatchAsync(order, cancellationToken);
}
catch (AggregateException ex)
{
    // One or more handlers failed
    var error = ErrorFactory.Create(ex);
    logger.LogError("Event dispatch failed: {Error}", error.Message);
    // All handlers ran, but some failed - handle appropriately
}
```

### ResponseWrapper Factory Methods and Pattern Matching

Use the static factory methods on the non-generic `ResponseWrapper` class, then pattern-match on the public nested records:

```csharp
// Void (no return value)
ResponseWrapper.Ok()            // success
ResponseWrapper.Fail(error)     // failure

// With a value
ResponseWrapper.Ok(myValue)     // ResponseWrapper<T> success — type inferred
ResponseWrapper.Fail<T>(error)  // ResponseWrapper<T> failure — explicit type arg required
```

```csharp
var result = await handler.Handle(command);

return result switch
{
    ResponseWrapper<Order>.Success success => Ok(success.Value),
    ResponseWrapper<Order>.Failure failure => BadRequest(failure.Error),
    _ => StatusCode(500)
};
```

### Error Factory

**Create errors from exceptions:**

```csharp
// From BusinessException
try
{
    // business logic
}
catch (InsufficientStockException ex)
{
    var error = ErrorFactory.Create(ex);
    return ResponseWrapper.Fail(error);
}

// From AggregateException (multiple errors)
catch (AggregateException ex)
{
    var error = ErrorFactory.Create(ex);
    return ResponseWrapper.Fail(error);
}

// From any exception
catch (Exception ex)
{
    var error = ErrorFactory.Create(ex);
    return ResponseWrapper.Fail(error);
}

// Custom error
var error = ErrorFactory.Create("VALIDATION_ERROR", "Email is required");
return ResponseWrapper.Fail(error);
```

## Architecture

```
┌─────────────────────────────────────────┐
│   Zwedze.Aetherweave.Application        │
├─────────────────────────────────────────┤
│                                         │
│  CQRS                                   │
│  ├── ICommandHandler<TReq, TRes>        │
│  └── IQueryHandler<TReq, TRes>          │
|  └── ResponseWrapper<T>                 |   
│                                         │
│  DomainEventHandlers                    │
│  ├── IDomainEventDispatcher             │
│  ├── DomainEventDispatcher              │
│  └── DomainEventHandlerRegistry         │
│                                         │   
│  Errors                                 │
│  └── ErrorFactory                       │
│  └── Error                              │
│                                         │
└─────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────┐
│   Zwedze.Aetherweave.SharedKernel       │
│   (Domain primitives)                   │
└─────────────────────────────────────────┘
```

## Best Practices

### 1. Domain Events

✅ **DO:**

- Dispatch events AFTER transaction commits
- Use domain events for cross-aggregate communication
- Keep handlers independent and idempotent
- Name events in past tense (OrderCreated, not CreateOrder)

❌ **DON'T:**

- Dispatch events before persistence
- Put business logic in event handlers
- Make handlers depend on each other

### 2. CQRS

✅ **DO:**

- Keep commands focused on single operations
- Use queries for read operations
- Return DTOs from queries, not domain entities
- Validate in handlers

❌ **DON'T:**

- Modify state in query handlers
- Return domain entities directly
- Mix command and query logic

### 3. Error Handling

✅ **DO:**

- Use BusinessException for domain errors
- Use ErrorFactory for consistent error creation
- Include error codes for client handling
- Return failures through ResponseWrapper

❌ **DON'T:**

- Throw exceptions for business rule violations in handlers
- Return null for failures
- Leak infrastructure exceptions to clients

## Dependencies

- `Zwedze.Aetherweave.SharedKernel` - Domain primitives
