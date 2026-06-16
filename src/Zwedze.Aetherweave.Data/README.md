# Aetherweave.Data

Core data persistence abstractions for the Aetherweave framework. Unit of Work interfaces consumed by application-layer code.

## Features

- **IUnitOfWork** - Save-changes contract with `IDisposable` / `IAsyncDisposable` support
- **ITransactionalUnitOfWork** - Extends `IUnitOfWork` with explicit `Commit` and `Rollback`
- **IUnitOfWorkFactory** - Factory for obtaining a transactional unit of work from DI

## Installation

```bash
dotnet add package Zwedze.Aetherweave.Data
```

> This package contains **only abstractions**. For a concrete EF Core implementation add `Zwedze.Aetherweave.Data.Relational`.

## API

### IUnitOfWork

```csharp
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task SaveChanges(CancellationToken cancellationToken = default);
}
```

Tracks pending changes and flushes them to the underlying store. Always consume with `await using` so disposal is guaranteed even on exceptions.

### ITransactionalUnitOfWork

```csharp
public interface ITransactionalUnitOfWork : IUnitOfWork
{
    Task Commit(CancellationToken cancellationToken = default);
    Task Rollback(CancellationToken cancellationToken = default);
}
```

Wraps changes in a database transaction. `Commit` **must** be called explicitly. If the unit of work is disposed without a commit, the transaction rolls back automatically.

### IUnitOfWorkFactory

```csharp
public interface IUnitOfWorkFactory
{
    ITransactionalUnitOfWork CreateTransactional();
}
```

## Usage

```csharp
public sealed class CreateOrderHandler(IUnitOfWorkFactory uowFactory) : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<ResponseWrapper<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        await using var uow = uowFactory.CreateTransactional();

        // ... persist changes ...
        await uow.SaveChanges(ct);
        await uow.Commit(ct);    // required - omitting causes automatic rollback on disposal

        return ResponseWrapper.Ok(orderId);
    }
}
```

For **read-only queries** inject the `DbContext` (or repository) directly. No unit of work is needed.

## Dependencies

No external dependencies. Pure abstractions.
