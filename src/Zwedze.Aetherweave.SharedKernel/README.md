# Zwedze.Aetherweave.SharedKernel

Domain-Driven Design primitives for the **domain layer** of a .NET 10 application: strongly-typed identifiers, aggregate roots, domain events and business exceptions.

This package has no infrastructure dependency (no EF Core, no ASP.NET). Reference it from the domain project; the other Aetherweave packages build on it.

```bash
dotnet add package Zwedze.Aetherweave.SharedKernel
```

---

## Why this package exists

| Problem | What SharedKernel gives you |
|---|---|
| `long orderId, long customerId` parameters are easy to swap by mistake | `Id<T>`: an `Id<Order>` cannot be passed where an `Id<Customer>` is expected (compile error) |
| Internal database keys leak into public APIs | Two identifiers per aggregate: `Id<T>` (internal) and `Code<T>` (public, external) |
| Side effects (emails, notifications) get mixed into domain logic | Aggregates *raise* `DomainEvent`s; handlers in the application layer react to them after persistence |
| Domain rule violations are surfaced as generic exceptions with no machine-readable code | `BusinessException` always carries an `ErrorCode`, which `Zwedze.Aetherweave.Application` turns into an API error |

---

## Public API at a glance

Namespace `Zwedze.Aetherweave.SharedKernel`:

| Type | Kind | Purpose |
|---|---|---|
| `Id<T>` | `readonly struct` | Internal identifier, wraps a `long`. Throws on `<= 0`. |
| `Code<T>` | `readonly struct` | External identifier / natural key, wraps a `string`. Throws on null, empty or whitespace. |
| `ErrorCode` | `readonly struct` | Machine-readable code for a business error, wraps a `string`. Throws on null, empty or whitespace. |
| `AggregateRoot<T>` | abstract class | Base class for every aggregate root. Holds `Id<T>` + `Code<T>`, tracks domain events, equality by `Id`. |
| `BusinessException` | abstract class | Base class for every domain rule violation. Carries an `ErrorCode`. |
| `IdExtensions.ToId<T>(this long)` | extension | `123L.ToId<Order>()` → `Id<Order>` |
| `CodeExtensions.ToCode<T>(this string)` | extension | `"ORD-1".ToCode<Order>()` → `Code<Order>` |

Namespace `Zwedze.Aetherweave.SharedKernel.DomainEvents`:

| Type | Kind | Purpose |
|---|---|---|
| `IDomainEvent` | interface | `Guid Id`, `DateTimeOffset OccurredOn` |
| `DomainEvent` | abstract record | Default implementation: `Id = Guid.NewGuid()`, `OccurredOn = DateTimeOffset.UtcNow`. Inherit from this. |
| `IHasDomainEvents` | interface | `IReadOnlyCollection<IDomainEvent> PopDomainEvents()`: returns pending events **and clears them**. |
| `IDomainEventHandler<TEvent>` | interface | `Task HandleAsync(TEvent @event, CancellationToken cancellationToken)`. Implemented in the application layer. |

---

## Core rules

Follow these rules when writing code with this package.

### Identifiers

1. **Every aggregate root has both an `Id<T>` and a `Code<T>`.** Both are mandatory constructor arguments of `AggregateRoot<T>`. There is no Id-only variant.
2. **`Id<T>` is internal only.** Use it for persistence keys, foreign keys between aggregates and domain logic. It never crosses the API boundary (routes, request/response DTOs, messages).
3. **`Code<T>` is the external identifier.** Use it in every external contract. In DTOs it is a plain `string`; convert to `Code<T>` at the boundary (controller or command).
4. **Never use raw `long` / `string` for an entity reference** in the domain or application layer. Use `Id<T>` / `Code<T>`. For optional references use `Id<T>?`, never `long?`.
5. **Create identifiers with factories:** `Id<T>.From(long)`, `Code<T>.From(string)`, or the `ToId<T>()` / `ToCode<T>()` extensions. Explicit casts (`(Id<Order>)123L`) also work, but prefer the factories.
6. **Never use `default(Id<T>)`, `default(Code<T>)` or `default(ErrorCode)`.** `default` bypasses validation: an `Id` of `0`, or a `Code` wrapping `null` (whose `GetHashCode()` throws).
7. In production code, get new values from `Zwedze.Aetherweave.IdentityGenerators`: `IIdentityGenerator.CreateId<T>()` (Snowflake `long`) and `CreateCode<T>()` (UUID v7 string). Do not invent your own generation.

Conversion back to primitives is **implicit** (`long l = id;`, `string s = code;`), so no `.Value` property exists and none is needed. The opposite direction is explicit only. Type safety is therefore one-way: an `Id<T>` can be passed silently to a `long` parameter, but a `long` cannot become an `Id<T>` by accident.

`ToString()` returns the raw value (`"123"`, `"ORD-001"`), so identifiers are safe to interpolate in messages and logs.

### Aggregates

8. **Constructor is `private`.** Expose two static factories:
   - `Create(...)`: builds a new instance, enforces invariants, and raises the creation event.
   - `Reconstitute(...)`: rebuilds an instance loaded from persistence. **Never raises events.**
9. **Properties use `private set`.** Only the aggregate mutates its own state, through intention-revealing methods (`Submit()`, `Cancel(reason)`), never through setters.
10. **Each mutating method:** (a) checks invariants and throws a `BusinessException` subclass if one is violated, (b) changes state, (c) calls `RaiseDomainEvent(...)` last.
11. **Child entities** (entities that belong to the aggregate, such as `OrderLine` in `Order`) are plain classes owned and mutated only by the root. They do not derive from `AggregateRoot<T>` and do not raise events themselves: the root raises them.
12. **Reference other aggregates by `Id<T>`**, never by object reference.
13. **Equality is identity-based:** two instances are equal when they have the same runtime type and the same `Id`. `Code` and state are ignored.

### Domain events

14. Declare events as `sealed record`s deriving from `DomainEvent`, named `<Noun><PastTense>Event` (`OrderSubmittedEvent`).
15. Keep payloads small: identifiers (`Id<T>`) and the few values handlers need. Do not put whole aggregates or mutable objects in events.
16. Raise events only for meaningful business transitions, not for every property change.
17. **Do not call `PopDomainEvents()` yourself** in domain or handler code. It drains the list; the dispatcher (`IDomainEventDispatcher` in `Zwedze.Aetherweave.Application`) calls it **after** the unit of work is committed. Calling it earlier loses the events.

### Business exceptions

18. Every domain rule violation is a `sealed` subclass of `BusinessException`, named `<Noun><Condition>Exception` (`OrderAlreadySubmittedException`).
19. Constructor signature: `BusinessException(ErrorCode errorCode, string message)`, **code first**.
20. Error code format: `RESOURCE_CONDITION`, an upper-case resource prefix plus the condition resulting in the error (`ORDER_ALREADY-SUBMITTED`, `ORDER_CANCELED`). Codes must be unique across the application. If a condition is a multi-word phrase: use dash to separate words (`ORDER_ALREADY-SUBMITTED`), not underscore (`ORDER_ALREADY_SUBMITTED`).
21. `"__unmanaged_error"` (`ErrorCode.UnmanagedErrorCode`) is reserved for unhandled, non-business exceptions. Never use it in your own exceptions.
22. Expose the relevant identifiers or values as properties, for logging and debugging.
23. Use `BusinessException` only for expected domain rule violations. Programming errors (null arguments, invalid state of the code itself) stay as standard .NET exceptions.
24. All ErrorCodes must be registered in a static class `BusinessRuleErrorCodes` in the domain layer so that they can be discovered and documented. Never create the code inline in the exception constructor with `ErrorCode.New("RESOURCE_CONDITION")`, rather `BusinessRuleErrorCodes.ResourceCondition`. Members are `public static readonly ErrorCode` fields named in PascalCase; only their value uses the `RESOURCE_CONDITION` format. Application handlers reference the same class (the application layer references the domain).

---

## Canonical example

A complete aggregate that follows every rule above.

```csharp
using Zwedze.Aetherweave.SharedKernel;
using Zwedze.Aetherweave.SharedKernel.DomainEvents;

// Domain/Orders/Order.cs
public sealed class Order : AggregateRoot<Order>
{
    private readonly List<OrderLine> _lines = [];

    public Id<Customer> CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines;
    public decimal Total => _lines.Sum(l => l.UnitPrice * l.Quantity);

    private Order(Id<Order> id, Code<Order> code, Id<Customer> customerId, OrderStatus status)
        : base(id, code)
    {
        CustomerId = customerId;
        Status = status;
    }

    public static Order Create(Id<Order> id, Code<Order> code, Id<Customer> customerId)
    {
        var order = new Order(id, code, customerId, OrderStatus.Draft);
        order.RaiseDomainEvent(new OrderCreatedEvent(order.Id, customerId));
        return order;
    }

    public static Order Reconstitute(
        Id<Order> id,
        Code<Order> code,
        Id<Customer> customerId,
        OrderStatus status,
        IEnumerable<OrderLine> lines)
    {
        var order = new Order(id, code, customerId, status);
        order._lines.AddRange(lines);
        return order; // no event: nothing happened, we only loaded it
    }

    public void AddLine(Id<Product> productId, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Draft)
        {
            throw new OrderNotEditableException(Id, Status);
        }

        _lines.Add(new OrderLine(productId, quantity, unitPrice));
    }

    public void Submit()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new OrderNotEditableException(Id, Status);
        }

        if (_lines.Count == 0)
        {
            throw new OrderEmptyException(Id);
        }

        Status = OrderStatus.Submitted;
        RaiseDomainEvent(new OrderSubmittedEvent(Id, Total));
    }
}

// Domain/Orders/OrderLine.cs (child entity: plain class, owned by Order)
public sealed class OrderLine(Id<Product> productId, int quantity, decimal unitPrice)
{
    public Id<Product> ProductId { get; } = productId;
    public int Quantity { get; } = quantity;
    public decimal UnitPrice { get; } = unitPrice;
}

// Domain/Orders/Events/OrderSubmittedEvent.cs
public sealed record OrderSubmittedEvent(Id<Order> OrderId, decimal Total) : DomainEvent;

// Domain/BusinessRuleErrorCodes.cs (single catalog of every error code)
public static class BusinessRuleErrorCodes
{
    public static readonly ErrorCode OrderEmpty = ErrorCode.New("ORDER_EMPTY");
    public static readonly ErrorCode OrderNotEditable = ErrorCode.New("ORDER_NOT-EDITABLE");
}

// Domain/Orders/Exceptions/OrderEmptyException.cs
public sealed class OrderEmptyException(Id<Order> orderId)
    : BusinessException(BusinessRuleErrorCodes.OrderEmpty, $"Order {orderId} has no lines and cannot be submitted.")
{
    public Id<Order> OrderId { get; } = orderId;
}

// Domain/Orders/Exceptions/OrderNotEditableException.cs
public sealed class OrderNotEditableException(Id<Order> orderId, OrderStatus status)
    : BusinessException(BusinessRuleErrorCodes.OrderNotEditable, $"Order {orderId} is {status} and can no longer be modified.")
{
    public Id<Order> OrderId { get; } = orderId;
    public OrderStatus Status { get; } = status;
}
```

`OrderStatus` is typically a `[SmartEnum]` from `Zwedze.Aetherweave.Generators`.

---

## How it fits with the other Aetherweave packages

```
Domain project          → Zwedze.Aetherweave.SharedKernel   (this package)
Application project     → Zwedze.Aetherweave.Application    (dispatcher, ErrorFactory, CQRS)
Infrastructure project  → Zwedze.Aetherweave.Data.Relational, IdentityGenerators
```

### Generating identifiers (`Zwedze.Aetherweave.IdentityGenerators`)

```csharp
var order = Order.Create(
    identityGenerator.CreateId<Order>(),   // Id<Order>, Snowflake
    identityGenerator.CreateCode<Order>(), // Code<Order>, UUID v7
    customerId);
```

### Dispatching domain events (`Zwedze.Aetherweave.Application`)

Events are dispatched **after** the transaction is committed, so handlers only see persisted state.

```csharp
await using var uow = unitOfWorkFactory.CreateTransactional();
var order = await orderRepository.GetByCode(command.OrderCode, ct);
order.Submit();
await orderRepository.Update(order, ct);
await uow.SaveChanges(ct);
await uow.Commit(ct);

await eventDispatcher.DispatchAsync(order, ct); // calls order.PopDomainEvents()
```

Handlers implement `IDomainEventHandler<TEvent>` and live in the application layer:

```csharp
public sealed class NotifyCustomerOnOrderSubmittedHandler(INotifier notifier)
    : IDomainEventHandler<OrderSubmittedEvent>
{
    public async Task HandleAsync(OrderSubmittedEvent @event, CancellationToken cancellationToken)
    {
        await notifier.OrderSubmitted(@event.OrderId, @event.Total, cancellationToken);
    }
}
```

Registration needs **both** the dispatcher mapping and a DI registration for the handler itself, because the dispatcher resolves handlers from the container:

```csharp
services.AddScoped<NotifyCustomerOnOrderSubmittedHandler>();
services.AddAetherweaveDomainEventDispatcher(registry =>
    registry.AddHandler<OrderSubmittedEvent, NotifyCustomerOnOrderSubmittedHandler>());
```

### Turning exceptions into API errors (`Zwedze.Aetherweave.Application`)

```csharp
catch (BusinessException ex)
{
    return ResponseWrapper.Fail<OrderDto>(ErrorFactory.Create(ex)); // Error { Codes = ["ORDER_EMPTY"], Message = ... }
}
```

`ErrorFactory.Create(Exception)` maps any non-business exception to `ErrorCode.UnmanagedErrorCode`. `ErrorFactory.Create(AggregateException)` (for example from a failed dispatch) merges the codes of all inner business exceptions.

### Persistence

Aggregates are not EF Core entities. The infrastructure layer maps an EF record to the domain and back:

- record → domain: always through `Reconstitute(...)`, wrapping primitives with `Id<T>.From(...)` / `Code<T>.From(...)`;
- domain → record: assign directly (`Id = order.Id`, `Code = order.Code`), using the implicit conversion to `long` / `string`.

---

## Anti-patterns

| Don't | Do instead |
|---|---|
| `public void Ship(long orderId)` | `public void Ship(Id<Order> orderId)` |
| Return `Id` in a response DTO, or accept it in a route | Use `Code` (as `string`) externally |
| `order.Status = OrderStatus.Cancelled;` (public setter) | `order.Cancel(reason);` |
| `RaiseDomainEvent(...)` in the constructor or in `Reconstitute` | Raise it in `Create` and in mutating methods |
| `RaiseDomainEvent(...)` before the state change or before the guard | Guard → mutate → raise |
| `new OrderTotalChangedEvent(old, new)` for every change | One event per business transition (`OrderSubmittedEvent`) |
| `public Order Customer { get; }` (reference another aggregate) | `public Id<Customer> CustomerId { get; }` |
| `throw new InvalidOperationException("order is empty")` for a domain rule | `throw new OrderEmptyException(Id)` |
| `BusinessException("message", "CODE")` (old argument order) | `BusinessException(BusinessRuleErrorCodes.OrderEmpty, "message")` |
| `ErrorCode.New("ORDER_EMPTY")` inline in an exception or handler | Declare it once in `BusinessRuleErrorCodes` and reference the field |
| `ORDER_001` (sequential number) or `ORDER_ALREADY_SUBMITTED` (underscores between words) | `ORDER_ALREADY-SUBMITTED` |
| `BusinessRuleErrorCodes.Order_Already_Submitted` (underscores in member name) | `BusinessRuleErrorCodes.OrderAlreadySubmitted` |
| Calling `PopDomainEvents()` before commit | Let `IDomainEventDispatcher.DispatchAsync` do it after commit |
| `default(Id<Order>)` as a placeholder | Use `Id<Order>?` and `null` |
