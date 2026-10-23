# Zwedze.Aetherweave.Data

Persistence **abstractions** for the application layer of a .NET 10 application: the transactional unit of work, its factory, and the exceptions of its contract.

This package has no dependency (no EF Core). Application code depends on it; the EF Core implementation lives in `Zwedze.Aetherweave.Data.Relational`, referenced only by the infrastructure and host projects.

```bash
dotnet add package Zwedze.Aetherweave.Data
```

---

## Why this package exists

| Problem | What Data gives you |
|---|---|
| Command handlers depend on `DbContext`, so EF Core leaks into the application layer | Handlers depend on `IUnitOfWorkFactory` only; EF Core stays in the infrastructure |
| Transactions are opened, committed and rolled back by hand, and forgotten commits go unnoticed | One explicit lifecycle: lazy start, explicit `Commit`, automatic rollback (with a warning) on disposal |
| Partial writes across several `SaveChanges` calls | Every `SaveChanges` of a unit of work runs in the same transaction |

---

## Public API at a glance

Namespace `Zwedze.Aetherweave.Data`:

| Type | Members |
|---|---|
| `IUnitOfWorkFactory` | `ITransactionalUnitOfWork CreateTransactional()` |
| `ITransactionalUnitOfWork` | `Task SaveChanges(CancellationToken ct)`, `Task Commit(CancellationToken ct)`, `Task Rollback(CancellationToken ct)`, `Dispose()`, `DisposeAsync()` |
| `IUnitOfWork` | Base interface: `Task SaveChanges(CancellationToken ct)`, `IDisposable`, `IAsyncDisposable`. Not created directly. |

Namespace `Zwedze.Aetherweave.Data.Exceptions` (all derive from `AetherweaveDataException`):

| Exception | Thrown when |
|---|---|
| `NoTransactionException` | `Commit` is called before any `SaveChanges` (no transaction was started) |
| `TransactionAlreadyCommittedException` | `SaveChanges`, `Commit` or `Rollback` is called after `Commit` |
| `TransactionAlreadyRolledBackException` | `SaveChanges`, `Commit` or `Rollback` is called after `Rollback` |
| `TransactionNotCommittedException` | Reserved; not thrown by the current implementation (an uncommitted disposal logs a warning instead) |

`CancellationToken ct` has no default value: callers always pass a token explicitly.

`ObjectDisposedException` is thrown when a disposed unit of work is used.

---

## Lifecycle

```
CreateTransactional()
   │
   ▼
 Active ──SaveChanges(ct)──► Active (transaction started on the first call, reused afterwards)
   │
   ├──Commit(ct)────► Committed   (terminal)
   ├──Rollback(ct)──► RolledBack  (terminal, change tracker cleared)
   └──Dispose──────► rolled back automatically + warning logged, change tracker cleared
```

| Situation | Behaviour |
|---|---|
| First `SaveChanges` | Starts the transaction, then writes |
| Next `SaveChanges` calls | Write inside the same transaction |
| `Commit` after at least one `SaveChanges` | Commits; the unit of work is finished |
| `Commit` without any `SaveChanges` | Throws `NoTransactionException` |
| `Rollback` | Rolls back the transaction if one was started, **clears EF's change tracker** (pending and saved-but-uncommitted entities are discarded); the unit of work is finished |
| Disposed while still active | Rolls back if a transaction was started, logs a warning, clears the change tracker |
| Automatic rollback on disposal fails | Logged as an error, **never thrown**: the exception that caused the disposal is preserved |
| Any call after `Commit` / `Rollback` | Throws `TransactionAlreadyCommittedException` / `TransactionAlreadyRolledBackException` |

---

## Core rules

1. **Every write goes through `CreateTransactional()`.** There is no non-transactional unit of work.
2. **Always `await using`** the unit of work, so disposal (and the automatic rollback) runs even when an exception is thrown.
3. **Call `Commit(ct)` explicitly** at the end of a successful write. A missing `Commit` silently loses the changes (only a warning is logged).
4. **Do not call `Rollback` in a `catch` block.** Disposal already rolls back on exceptions. `Rollback` is for an explicit business decision (for example, an external call refused the operation) and is terminal: calling it after `Commit`, or twice, throws.
5. **One unit of work at a time per DI scope.** All units of work of a scope share the same scoped `DbContext`; opening a second one while the first is still open fails.
6. **A unit of work is single-use.** After `Commit` or `Rollback`, create a new one for further writes.
7. **Reads need no unit of work.** Query handlers use a query service (or a repository) directly.
8. **Domain events are dispatched after `Commit`**, never inside the unit of work (see `Zwedze.Aetherweave.Application`).
9. **Application code depends on this package only**, never on `Zwedze.Aetherweave.Data.Relational` or `DbContext`. Catch the exceptions above from `Zwedze.Aetherweave.Data.Exceptions`.

---

## Canonical example

```csharp
// {MicroServiceName}.Application/Orders/Commands/SubmitOrder/SubmitOrderHandler.cs
internal sealed class SubmitOrderHandler(
    IOrderRepository orderRepository,
    IPaymentServiceClient paymentClient,
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

        order.Submit();

        await using var uow = uowFactory.CreateTransactional(); // rolled back on dispose if not committed
        await orderRepository.UpdateStatusAsync(order, ct);
        await uow.SaveChanges(ct);                              // starts the transaction

        var payment = await paymentClient.ProcessPaymentAsync(new PaymentRequest((string)order.Code), ct);
        if (!payment.IsSuccess)
        {
            await uow.Rollback(ct);                             // explicit business decision, terminal
            return ResponseWrapper.Fail<Code<Order>>(
                ErrorFactory.Create(BusinessRuleErrorCodes.PaymentRefused, $"Payment for order {order.Code} was refused."));
        }

        await uow.Commit(ct);                                   // mandatory

        await eventDispatcher.Dispatch(order);                  // after commit
        return ResponseWrapper.Ok(order.Code);
    }
}
```

(Business exceptions from `order.Submit()` are caught and converted as shown in the `Zwedze.Aetherweave.Application` README; omitted here.)

---

## How it fits with the other Aetherweave packages

```
Application project     → Zwedze.Aetherweave.Data             (this package: IUnitOfWorkFactory)
Infrastructure project  → Zwedze.Aetherweave.Data.Relational  (EF Core implementation, AddAetherweaveData<TDbContext>)
```

`AddAetherweaveData<TDbContext>(...)` (Data.Relational) registers `IUnitOfWorkFactory` as **scoped**, bound to the scoped `TDbContext`.

---

## Anti-patterns

| Don't | Do instead |
|---|---|
| `var uow = uowFactory.CreateTransactional();` without `using` | `await using var uow = uowFactory.CreateTransactional();` |
| Forgetting `Commit(ct)` | Always end a successful write with `await uow.Commit(ct)` |
| `catch { await uow.Rollback(ct); throw; }` | Let disposal roll back; only `throw` (or don't catch) |
| Reusing a unit of work after `Commit` / `Rollback` | Create a new one |
| Two units of work open at the same time in one scope | Finish (commit or dispose) the first one before creating the next |
| Opening a unit of work for a read | Read directly through a query service or repository |
| Injecting `DbContext` into a handler | Inject `IUnitOfWorkFactory` and repository interfaces |
| `SaveChanges(cancellationToken: default)` / omitting the token | Pass the handler's `ct` |
| Dispatching domain events before `Commit` | `Commit`, then `Dispatch` |
