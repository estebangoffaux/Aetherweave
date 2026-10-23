# Zwedze.Aetherweave.Application

Building blocks for the **application layer** of a .NET 10 clean-architecture application: CQRS handler contracts, a result type (`ResponseWrapper`), error creation from exceptions (`ErrorFactory`), and a domain event dispatcher.

It builds on `Zwedze.Aetherweave.SharedKernel` (aggregates, domain events, `BusinessException`, `ErrorCode`). Reference it from the application project.

```bash
dotnet add package Zwedze.Aetherweave.Application
```

---

## Why this package exists

| Problem | What Application gives you |
|---|---|
| Every use case invents its own service shape | One contract per use case: `ICommandHandler<,>` for writes, `IQueryHandler<,>` for reads |
| Failures travel as exceptions or `null` up to the API | Handlers return a `ResponseWrapper` that is either `Success` or `Failure(Error)`, and controllers pattern-match on it |
| Business errors reach clients as free text | `ErrorFactory` turns a `BusinessException` into an `Error` carrying its `ErrorCode`, and hides the details of unexpected exceptions |
| Side effects (emails, notifications) are coded inside use cases | Aggregates raise domain events; the dispatcher runs the registered `IDomainEventHandler<TEvent>`s after commit |

---

## Public API at a glance

Namespace `Zwedze.Aetherweave.Application.CQRS`:

| Type | Purpose |
|---|---|
| `ICommandHandler<TRequest, TResponse>` | Write use case returning a value: `Task<ResponseWrapper<TResponse>> Handle(TRequest request, CancellationToken ct)` |
| `ICommandHandler<TRequest>` | Write use case with no value: `Task<ResponseWrapper> Handle(TRequest request, CancellationToken ct)` |
| `IQueryHandler<TRequest, TResponse>` | Read use case with parameters: `Task<ResponseWrapper<TResponse>> Handle(TRequest request, CancellationToken ct)` |
| `IQueryHandler<TResponse>` | Read use case without parameters: `Task<ResponseWrapper<TResponse>> Handle(CancellationToken ct)` |
| `ResponseWrapper` | Result with no value. Nested records `Success` and `Failure(Error Error)`. Hosts **all** factory methods. |
| `ResponseWrapper<T>` | Result with a value. Nested records `Success(T Value)` and `Failure(Error Error)`. No factory methods of its own. |

`CancellationToken ct` has no default value: callers always pass a token explicitly.

Factory methods (all on the non-generic `ResponseWrapper`):

| Call | Returns |
|---|---|
| `ResponseWrapper.Ok()` | `ResponseWrapper` (success, no value) |
| `ResponseWrapper.Ok(value)` | `ResponseWrapper<T>` (success, `T` inferred from `value`) |
| `ResponseWrapper.Fail(error)` | `ResponseWrapper` (failure) |
| `ResponseWrapper.Fail<T>(error)` | `ResponseWrapper<T>` (failure, `T` must be given explicitly) |

Namespace `Zwedze.Aetherweave.Application.Errors`:

| Type | Purpose |
|---|---|
| `Error` | `sealed record` with `IReadOnlyCollection<ErrorCode> Codes` and `string Message`. Returned to clients. |
| `ErrorFactory.Create(ErrorCode code, string message)` | Builds an `Error` for a failure detected in a handler (entity not found, conflict...) |
| `ErrorFactory.Create(Exception ex)` | Builds an `Error` from any exception (see [Error creation rules](#error-creation-rules)) |

Namespace `Zwedze.Aetherweave.Application.DomainEventHandlers`:

| Type | Purpose |
|---|---|
| `IDomainEventDispatcher` | `Task Dispatch(IHasDomainEvents domainEventsHolder)`. **Scoped.** Takes no `CancellationToken` on purpose (see [Cancellation](#cancellation)). |
| `DomainEventHandlerRegistry` | Maps event types to handler types inside `AddAetherweaveDomainEventDispatcher` |

Namespace `Zwedze.Aetherweave.Application.Extensions`:

| Member | Purpose |
|---|---|
| `services.AddAetherweaveDomainEventDispatcher(Action<DomainEventHandlerRegistry>)` | Registers `IDomainEventDispatcher` (scoped) with the event → handler mappings |

---

## Core rules

### Handlers

1. **One handler per use case.** Commands change state, queries never do. Name them `<Verb><Noun>Command` / `<Verb><Noun>Handler` and `Get<Noun(s)>Query` / `Get<Noun(s)>Handler`.
2. **Handlers never let a business failure escape as an exception.** They return `ResponseWrapper.Fail(...)` / `ResponseWrapper.Fail<T>(...)`. A `BusinessException` thrown by the domain is caught in the handler and converted with `ErrorFactory.Create(ex)`.
3. **Failures detected by the handler itself** (referenced entity not found, code conflict...) use `ErrorFactory.Create(code, message)`, with the code taken from the application's `BusinessRuleErrorCodes` catalog. Never create an `ErrorCode` inline.
4. **The external contract uses `Code<T>`, never `Id<T>`.** Commands carry `Code<T>` to reference entities; create commands return the new entity's `Code<T>`; query DTOs expose codes as `string`.
5. **Queries return DTOs**, never domain aggregates.
6. **`CancellationToken` is named `ct`**, is the last parameter, has no default value, and is passed to every awaited call. The one exception is `IDomainEventDispatcher.Dispatch`, which deliberately takes no token.

### Writes and domain events

7. **Order of a write:** `CreateTransactional()` → repository add/update → `SaveChanges(ct)` → `Commit(ct)` → `Dispatch(aggregate)`. Events are dispatched **only after commit**, so handlers only see persisted state.
8. **Each domain event handler is registered twice:** once in DI as its own type (`services.AddScoped<THandler>()`), because the dispatcher resolves it with `GetRequiredService<THandler>()`, and once in the `AddAetherweaveDomainEventDispatcher` callback to map it to its event. A missing DI registration is not detected at startup: the dispatch fails (and is logged) the first time the event is raised.
9. **Call `AddAetherweaveDomainEventDispatcher` once.** A second call replaces the first registration, and the first call's mappings are lost.
10. **Register handlers for the concrete event type.** Dispatch matches the event's exact runtime type: a handler registered for a base event type or an interface is never called.
11. **Handlers must be independent and idempotent.** They must not rely on each other or on their execution order, and they must not contain business rules (those belong in the aggregate).

### Domain event dispatch behaviour

What `Dispatch(aggregate)` does, in order:

1. Calls `aggregate.PopDomainEvents()`, which returns the pending events and **clears them** from the aggregate.
2. For each event, in the order it was raised, runs every handler registered for its exact type, **sequentially**, in registration order. Each handler is resolved from the current DI scope.
3. If a handler throws, the remaining handlers of that event still run. The failures of an event are grouped into an `AggregateException`, which is **logged** (`LogError`) and **not rethrown**. Dispatch then continues with the next event.

Consequences:

- **`Dispatch` never throws because of a handler failure.** The caller cannot tell from the call that a side effect failed: do not wrap it in `try`/`catch` expecting handler errors, and do not base the handler's `ResponseWrapper` on it. The transaction is already committed, so the use case has succeeded.
- A failed side effect is visible **only in the logs**. A handler that must not silently fail has to deal with its own failures (retry, compensation, outbox...).
- An event with no registered handler is skipped, with a `Debug` log.

### Cancellation

`Dispatch` takes **no `CancellationToken`**, so the caller's token (HTTP request, message consumer...) can never reach event handlers. The transaction is already committed when dispatch runs: a client disconnecting or a message being abandoned must not cancel the side effects of a change that has already happened.

Instead, the dispatcher passes **`IHostApplicationLifetime.ApplicationStopped`** to every handler's `ct`. In practice handlers run to completion, bounded by the host's shutdown timeout.

| Situation | Behaviour |
|---|---|
| Application stopped (`ApplicationStopped` cancelled) | Checked before each handler. Dispatch stops: `Dispatch` throws `OperationCanceledException`, and the remaining handlers and events are **not** run |
| A handler throws `OperationCanceledException` for its own reason (internal timeout, linked token...) while the application is running | Treated as an ordinary handler failure: logged, and dispatch continues |

Consequences for event handler authors:

- Pass the `ct` you receive to every awaited call; it is the application lifetime token, not the request's.
- Need a time limit? Create it inside the handler (`CancellationTokenSource.CreateLinkedTokenSource(ct)` + `CancelAfter`). Its `OperationCanceledException` is logged as a failure of that handler only.

### Error creation rules

| Input of `ErrorFactory.Create` | `Error.Codes` | `Error.Message` |
|---|---|---|
| `(ErrorCode code, string message)` | `[code]` | `message` |
| `BusinessException` | `[ex.ErrorCode]` | `ex.Message` |
| `AggregateException` | The `ErrorCode` of every inner `BusinessException`, plus `__unmanaged_error` once if any inner exception is not a `BusinessException` | Business messages joined by new lines; the messages of other exceptions are **not** included |
| Any other exception | `[__unmanaged_error]` | `"An internal server error occurred. Please contact support."` |

- `Create(Exception ex)` checks the runtime type, so `catch (Exception ex) { ErrorFactory.Create(ex); }` handles all three cases correctly.
- Messages of unexpected exceptions never reach the `Error`, so they are not leaked to clients. **Log the exception yourself** before converting it if you need its details.
- Nested `AggregateException`s are not flattened: an inner `AggregateException` counts as a non-business exception. Call `ex.Flatten()` before `Create` if your exceptions can be nested.
- `__unmanaged_error` is `ErrorCode.UnmanagedErrorCode` (SharedKernel). It is reserved for this case and never used by application code.

---

## Canonical example

A complete write path following every rule above. The domain (`Order`, `OrderEmptyException`, `BusinessRuleErrorCodes`) is the one from the `Zwedze.Aetherweave.SharedKernel` README.

### Command and handler

```csharp
// {MicroServiceName}.Application/Orders/Commands/SubmitOrder/SubmitOrderCommand.cs
public sealed record SubmitOrderCommand(Code<Order> OrderCode);

// {MicroServiceName}.Application/Orders/Commands/SubmitOrder/SubmitOrderHandler.cs
internal sealed class SubmitOrderHandler(
    IOrderRepository orderRepository,
    IUnitOfWorkFactory uowFactory,
    IDomainEventDispatcher eventDispatcher
) : ICommandHandler<SubmitOrderCommand, Code<Order>>
{
    public async Task<ResponseWrapper<Code<Order>>> Handle(SubmitOrderCommand request, CancellationToken ct)
    {
        var order = await orderRepository.GetByCodeAsync(request.OrderCode, ct);
        if (order is null)
        {
            return ResponseWrapper.Fail<Code<Order>>(
                ErrorFactory.Create(BusinessRuleErrorCodes.OrderNotFound, $"Order {request.OrderCode} not found."));
        }

        try
        {
            order.Submit(); // may throw OrderEmptyException / OrderNotEditableException
        }
        catch (BusinessException ex)
        {
            return ResponseWrapper.Fail<Code<Order>>(ErrorFactory.Create(ex));
        }

        await using var uow = uowFactory.CreateTransactional();
        await orderRepository.UpdateStatusAsync(order, ct);
        await uow.SaveChanges(ct);
        await uow.Commit(ct);

        await eventDispatcher.Dispatch(order); // no ct: handlers get the application lifetime token

        return ResponseWrapper.Ok(order.Code);
    }
}
```

### Query and handler

```csharp
// {MicroServiceName}.Application/Orders/Queries/GetOrders/GetOrdersHandler.cs
internal sealed class GetOrdersHandler(IOrderQueryService queryService)
    : IQueryHandler<GetOrdersQuery, OrderListDto>
{
    public async Task<ResponseWrapper<OrderListDto>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        var orders = await queryService.GetAllAsync(request.CustomerCode, ct);
        return ResponseWrapper.Ok(orders);
    }
}
```

### Domain event handler

```csharp
// {MicroServiceName}.Application/Orders/DomainEventHandlers/NotifyCustomerOnOrderSubmittedHandler.cs
internal sealed class NotifyCustomerOnOrderSubmittedHandler(INotifier notifier)
    : IDomainEventHandler<OrderSubmittedEvent>
{
    public async Task HandleAsync(OrderSubmittedEvent @event, CancellationToken ct)
    {
        await notifier.OrderSubmitted(@event.OrderId, @event.Total, ct);
    }
}
```

### Registration

```csharp
// {MicroServiceName}.Application/ServiceCollectionExtensions.cs
services.AddScoped<ICommandHandler<SubmitOrderCommand, Code<Order>>, SubmitOrderHandler>();
services.AddScoped<IQueryHandler<GetOrdersQuery, OrderListDto>, GetOrdersHandler>();

// Domain event handlers: DI registration + dispatcher mapping
services.AddScoped<NotifyCustomerOnOrderSubmittedHandler>();
services.AddScoped<UpdateStatisticsOnOrderSubmittedHandler>();

services.AddAetherweaveDomainEventDispatcher(registry =>
{
    // Several handlers for one event, then chain to the next event
    registry
        .Configure<OrderSubmittedEvent>()
            .AddHandler<NotifyCustomerOnOrderSubmittedHandler>()
            .AddHandler<UpdateStatisticsOnOrderSubmittedHandler>()
        .Configure<OrderCancelledEvent>()
            .AddHandler<RefundOnOrderCancelledHandler>();

    // Shorthand for a single handler
    registry.AddHandler<OrderCreatedEvent, NotifySalesOnOrderCreatedHandler>();
});
```

The dispatcher needs `ILogger<T>` and `IHostApplicationLifetime`. ASP.NET Core, worker services and the generic host register both by default. An application without a host (a bare `ServiceCollection`) fails with a DI error the first time `IDomainEventDispatcher` is resolved.

### Controller

```csharp
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

- Always keep the `_` arm: the compiler cannot prove that `Success` and `Failure` are the only subtypes.
- `Ok(object?)` takes an `object`, so the implicit `Code<T>` → `string` conversion does not apply there: cast explicitly with `(string)s.Value`.
- For `ResponseWrapper` (no value), match `ResponseWrapper.Success` → `NoContent()` and `ResponseWrapper.Failure f` → `BadRequest(f.Error)`.

---

## How it fits with the other Aetherweave packages

```
Domain project          → Zwedze.Aetherweave.SharedKernel   (aggregates, events, BusinessException, ErrorCode)
Application project     → Zwedze.Aetherweave.Application    (this package)
                        → Zwedze.Aetherweave.Data            (IUnitOfWorkFactory, used by command handlers)
Infrastructure project  → Zwedze.Aetherweave.Data.Relational, IdentityGenerators
```

---

## Anti-patterns

| Don't | Do instead |
|---|---|
| `ResponseWrapper<T>.Ok(value)` / `ResponseWrapper<T>.Fail(error)` | `ResponseWrapper.Ok(value)` / `ResponseWrapper.Fail<T>(error)` |
| `handler.Handle(command)` (no token) | `handler.Handle(command, ct)` |
| `Handle(TRequest request, CancellationToken cancellationToken = default)` | `Handle(TRequest request, CancellationToken ct)` |
| `ErrorFactory.Create("ORDER_NOT_FOUND", "...")` (string, inline code) | `ErrorFactory.Create(BusinessRuleErrorCodes.OrderNotFound, "...")` |
| Letting a `BusinessException` escape the handler | Catch it and return `ResponseWrapper.Fail<T>(ErrorFactory.Create(ex))` |
| `Dispatch` before `Commit` | `Commit`, then `Dispatch` |
| `try { await Dispatch(...) } catch (AggregateException)` | Nothing to catch: failures are logged by the dispatcher. Handle failures inside the event handler |
| Forwarding the request's token to side effects (custom dispatch, `Task.Run(..., ct)`) | Let `Dispatch` give handlers the application lifetime token |
| Time-limiting a handler with the received `ct` alone | Link it: `CreateLinkedTokenSource(ct)` + `CancelAfter(...)` inside the handler |
| Mapping a handler in the registry only | Also `services.AddScoped<THandler>()` |
| Registering a handler for a base event type | Register it for each concrete event type |
| Returning `order.Id` or a domain aggregate from a handler | Return `order.Code` or a DTO |
| Putting business rules in a domain event handler | Put them in the aggregate; handlers only run side effects |
| Returning `ex.Message` of an unexpected exception to clients | `ErrorFactory.Create(ex)` (hides it) and log the exception |
