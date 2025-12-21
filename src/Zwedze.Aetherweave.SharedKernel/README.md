# Aetherweave.SharedKernel

Domain-Driven Design building blocks and primitives for building clean, maintainable domain models.

## Features

- 🏛️ **Aggregate Root** - Base class for domain aggregates with built-in domain event support
- 🎯 **Type-Safe IDs** - Generic value objects for entity identifiers
- 🏷️ **Business Codes** - Type-safe business identifiers and natural keys
- ⚡ **Domain Events** - Event infrastructure for domain event patterns
- 🛡️ **Business Exceptions** - Base exception class for domain errors

## Installation

```bash
dotnet add package Zwedze.Aetherweave.SharedKernel
```

## Quick Start

### 1. Value Objects - Id<T> and Code<T>

**Type-safe entity identifiers:**

```csharp
// Type-safe IDs prevent mixing different entity types
var orderId = (Id<Order>)123L;
var customerId = (Id<Customer>)456L;

// orderId = customerId; // Compile error! ✅

// Alternative: factory method (cleaner)
var productId = Id<Product>.From(789);
```

**Type-safe business codes:**

```csharp
// Natural keys and business identifiers
var orderCode = (Code<Order>)"ORD-2024-001";
var sku = (Code<Product>)"WIDGET-A1";

// Alternative: factory method
var invoiceCode = Code<Invoice>.From("INV-2024-12345");
```

**Benefits:**
- ✅ Compile-time type safety
- ✅ No mixing IDs from different entities
- ✅ Clear intent in code
- ✅ Validation built-in (no zero/negative IDs, no null/empty codes)

### 2. Aggregate Root

**Define your domain aggregates:**

```csharp
public class Order : AggregateRoot<Order>
{
    public decimal Total { get; private set; }
    public OrderStatus Status { get; private set; }
    private readonly List<OrderItem> _items = [];

    public Order(Id<Order> id, Code<Order> orderNumber) 
        : base(id, orderNumber)
    {
        Status = OrderStatus.Draft;
    }

    public void AddItem(Product product, int quantity)
    {
        var item = new OrderItem(product, quantity);
        _items.Add(item);
        Total += item.Price;

        // Raise domain event
        RaiseDomainEvent(new OrderItemAddedEvent(Id, product.Id, quantity));
    }

    public void Submit()
    {
        if (!_items.Any())
            throw new OrderEmptyException();

        Status = OrderStatus.Submitted;
        
        // Raise domain event
        RaiseDomainEvent(new OrderSubmittedEvent(Id, Total));
    }
}
```

**Aggregate equality:**

```csharp
var order1 = new Order(Id<Order>.From(1), Code<Order>.From("ORD-001"));
var order2 = new Order(Id<Order>.From(1), Code<Order>.From("ORD-002"));

// Equality based on Id
order1 == order2  // true - same entity (same Id)
order1.Equals(order2)  // true
```

### 3. Domain Events

**Define domain events:**

```csharp
public record OrderSubmittedEvent(Id<Order> OrderId, decimal Total) : DomainEvent;

public record OrderItemAddedEvent(
    Id<Order> OrderId, 
    Id<Product> ProductId, 
    int Quantity) : DomainEvent;

public record OrderCancelledEvent(Id<Order> OrderId, string Reason) : DomainEvent;
```

**Raise events from aggregates:**

```csharp
public class Order : AggregateRoot<Order>
{
    public void Cancel(string reason)
    {
        Status = OrderStatus.Cancelled;
        
        // Event automatically tracked
        RaiseDomainEvent(new OrderCancelledEvent(Id, reason));
    }
}
```

**Dispatch events after persistence:**

```csharp
public class OrderService(
    IOrderRepository orderRepository,
    IDomainEventDispatcher eventDispatcher)
{
    public async Task SubmitOrder(Id<Order> orderId, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(orderId, ct);
        order.Submit();
        
        await orderRepository.SaveAsync(order, ct);
        
        // Pop and dispatch events
        await eventDispatcher.DispatchAsync(order, ct);
    }
}
```

### 4. Business Exceptions

**Define domain exceptions:**

```csharp
public class InsufficientStockException(int requested, int available) 
    : BusinessException(
        $"Insufficient stock. Requested: {requested}, Available: {available}",
        "STOCK_001")
{
    public int Requested { get; } = requested;
    public int Available { get; } = available;
}

public class OrderNotFoundException(Id<Order> orderId) 
    : BusinessException(
        $"Order {orderId} not found",
        "ORDER_001")
{
    public Id<Order> OrderId { get; } = orderId;
}

public class InvalidOrderStateException(OrderStatus currentState, OrderStatus requiredState) 
    : BusinessException(
        $"Order must be in {requiredState} state, but is in {currentState}",
        "ORDER_002")
{
    public OrderStatus CurrentState { get; } = currentState;
    public OrderStatus RequiredState { get; } = requiredState;
}
```

**Use in domain logic:**

```csharp
public class Product : AggregateRoot<Product>
{
    public int StockQuantity { get; private set; }

    public void ReserveStock(int quantity)
    {
        if (quantity > StockQuantity)
        {
            throw new InsufficientStockException(quantity, StockQuantity);
        }

        StockQuantity -= quantity;
        RaiseDomainEvent(new StockReservedEvent(Id, quantity));
    }
}
```

## Advanced Usage

### Custom Aggregate Root

**Without Code (Id only):**

If you don't need business codes, you can create a simpler base class:

```csharp
public abstract class AggregateRootWithIdOnly<TEntity>(Id<TEntity> id) : IHasDomainEvents
{
    private readonly List<IDomainEvent> _events = [];

    public Id<TEntity> Id { get; } = id;

    public ImmutableArray<IDomainEvent> PopDomainEvents()
    {
        var events = _events.ToImmutableArray();
        _events.Clear();
        return events;
    }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _events.Add(domainEvent);
    }

    // Equality based on Id...
}
```

### Value Object Conversions

**To primitives:**

```csharp
var orderId = Id<Order>.From(123);
long idValue = (long)orderId;  // Explicit cast to long

var orderCode = Code<Order>.From("ORD-001");
string codeValue = (string)orderCode;  // Explicit cast to string
```

**From primitives:**

```csharp
// Explicit cast
var id = (Id<Order>)123L;
var code = (Code<Order>)"ORD-001";

// Factory method (recommended)
var id2 = Id<Order>.From(123);
var code2 = Code<Order>.From("ORD-001");
```

### Domain Event Metadata

All domain events include:

```csharp
public interface IDomainEvent
{
    Guid Id { get; }              // Unique event ID
    DateTimeOffset OccurredOn { get; }  // When event occurred (UTC)
}
```

**Access in handlers (implemented in the Application layer):**

```csharp
public class OrderSubmittedHandler : IDomainEventHandler<OrderSubmittedEvent>
{
    public async Task HandleAsync(OrderSubmittedEvent @event, CancellationToken ct)
    {
        // Event metadata available
        var eventId = @event.Id;
        var occurredAt = @event.OccurredOn;
        
        // Event-specific data
        var orderId = @event.OrderId;
        var total = @event.Total;
        
        // Handle event...
    }
}
```

## Architecture Patterns

### Typical Aggregate Structure

```csharp
public class Order : AggregateRoot<Order>
{
    // 1. Private fields for encapsulation
    private readonly List<OrderItem> _items = [];
    
    // 2. Public properties (read-only from outside)
    public decimal Total { get; private set; }
    public OrderStatus Status { get; private set; }
    public Id<Customer> CustomerId { get; private set; }
    
    // 3. Constructor (factory method)
    public Order(Id<Order> id, Code<Order> orderNumber, Id<Customer> customerId) 
        : base(id, orderNumber)
    {
        CustomerId = customerId;
        Status = OrderStatus.Draft;
    }
    
    // 4. Business methods that enforce invariants
    public void AddItem(Product product, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOrderStateException(Status, OrderStatus.Draft);
            
        var item = new OrderItem(product, quantity);
        _items.Add(item);
        Total += item.Price;
        
        RaiseDomainEvent(new OrderItemAddedEvent(Id, product.Id, quantity));
    }
    
    public void Submit()
    {
        if (!_items.Any())
            throw new OrderEmptyException();
            
        if (Status != OrderStatus.Draft)
            throw new InvalidOrderStateException(Status, OrderStatus.Draft);
            
        Status = OrderStatus.Submitted;
        RaiseDomainEvent(new OrderSubmittedEvent(Id, Total));
    }
}
```

### Repository Pattern

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Id<Order> id, CancellationToken ct);
    Task<Order?> GetByCodeAsync(Code<Order> orderNumber, CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
    Task SaveAsync(Order order, CancellationToken ct);
}
```

## Best Practices

### ✅ DO

1. **Use Id<T> and Code<T> everywhere:**
   ```csharp
   // Good
   public void AssignOrder(Id<Order> orderId, Id<Driver> driverId) { }
   
   // Bad
   public void AssignOrder(long orderId, long driverId) { }  // Easy to mix up!
   ```

2. **Raise domain events for important state changes:**
   ```csharp
   public void ApproveOrder()
   {
       Status = OrderStatus.Approved;
       RaiseDomainEvent(new OrderApprovedEvent(Id));  // ✅
   }
   ```

3. **Keep aggregates consistent:**
   ```csharp
   public void Cancel(string reason)
   {
       // Validate
       if (Status == OrderStatus.Delivered)
           throw new OrderAlreadyDeliveredException();
           
       // Update state
       Status = OrderStatus.Cancelled;
       CancellationReason = reason;
       
       // Raise event
       RaiseDomainEvent(new OrderCancelledEvent(Id, reason));
   }
   ```

4. **Use business exceptions for domain errors:**
   ```csharp
   if (quantity > StockQuantity)
       throw new InsufficientStockException(quantity, StockQuantity);
   ```

### ❌ DON'T

1. **Don't bypass encapsulation:**
   ```csharp
   // Bad
   order.Status = OrderStatus.Cancelled;  // Public setter
   
   // Good
   order.Cancel(reason);  // Business method
   ```

2. **Don't raise events for every property change:**
   ```csharp
   // Bad - too granular
   RaiseDomainEvent(new OrderTotalChangedEvent(oldTotal, newTotal));
   
   // Good - meaningful business events
   RaiseDomainEvent(new OrderSubmittedEvent(Id, Total));
   ```

3. **Don't use primitive types for IDs:**
   ```csharp
   // Bad
   public void Ship(long orderId) { }
   
   // Good
   public void Ship(Id<Order> orderId) { }
   ```

4. **Don't make aggregates too large:**
   ```csharp
   // Bad - Order managing inventory, shipping, billing
   public class Order : AggregateRoot<Order>
   {
       public void ManageInventory() { }
       public void ProcessShipping() { }
       public void GenerateInvoice() { }
   }
   
   // Good - Focused aggregate
   public class Order : AggregateRoot<Order>
   {
       public void Submit() { }
       public void Cancel(string reason) { }
   }
   ```
