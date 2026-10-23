using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Zwedze.Aetherweave.Data.Exceptions;

namespace Zwedze.Aetherweave.Data.Relational.UnitOfWork;

internal sealed class TransactionalUnitOfWork(
    DbContext context,
    ILogger<TransactionalUnitOfWork> logger) : ITransactionalUnitOfWork
{
    private bool _disposed;
    private TransactionState _state = TransactionState.Active;
    private IDbContextTransaction? _transaction;

    public async Task SaveChanges(CancellationToken ct)
    {
        EnsureActive();

        // The transaction is lazy: it starts on the first SaveChanges
        _transaction ??= await context.Database.BeginTransactionAsync(ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task Commit(CancellationToken ct)
    {
        EnsureActive();

        if (_transaction is null)
        {
            throw new NoTransactionException();
        }

        await _transaction.CommitAsync(ct);
        _state = TransactionState.Committed;
    }

    public async Task Rollback(CancellationToken ct)
    {
        EnsureActive();

        try
        {
            if (_transaction is not null)
            {
                await _transaction.RollbackAsync(ct);
            }
        }
        finally
        {
            // Terminal even if the rollback fails: the database is the source of truth, the tracked state is discarded
            _state = TransactionState.RolledBack;
            context.ChangeTracker.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_state == TransactionState.Active)
        {
            if (_transaction is not null)
            {
                logger.LogWarning("Transaction was not committed and will be rolled back. " +
                    "Call Commit() explicitly to persist changes.");

                try
                {
                    _transaction.Rollback();
                }
                catch (Exception ex)
                {
                    // Never throw from disposal: it would hide the exception that led here
                    logger.LogError(ex, "Automatic rollback of the uncommitted transaction failed.");
                }
            }

            context.ChangeTracker.Clear();
        }

        _transaction?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_state == TransactionState.Active)
        {
            if (_transaction is not null)
            {
                logger.LogWarning("Transaction was not committed and will be rolled back. " +
                    "Call Commit() explicitly to persist changes.");

                try
                {
                    await _transaction.RollbackAsync();
                }
                catch (Exception ex)
                {
                    // Never throw from disposal: it would hide the exception that led here
                    logger.LogError(ex, "Automatic rollback of the uncommitted transaction failed.");
                }
            }

            context.ChangeTracker.Clear();
        }

        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }
    }

    private void EnsureActive()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        switch (_state)
        {
            case TransactionState.Committed:
                throw new TransactionAlreadyCommittedException();
            case TransactionState.RolledBack:
                throw new TransactionAlreadyRolledBackException();
        }
    }

    private enum TransactionState
    {
        Active = 0,
        Committed = 1,
        RolledBack = 2
    }
}
