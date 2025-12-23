using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Zwedze.Aetherweave.Data.Relational.Exceptions;

namespace Zwedze.Aetherweave.Data.Relational.UnitOfWork;

internal sealed class TransactionalUnitOfWork(
    DbContext context,
    ILogger<TransactionalUnitOfWork> _logger) : ITransactionalUnitOfWork
{
    private readonly DbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private bool _disposed;
    private IDbContextTransaction? _transaction;
    private bool _transactionCommitted;

    public async Task SaveChanges(CancellationToken cancellationToken = default)
    {
        await EnsureTransactionStarted(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Commit(CancellationToken cancellationToken = default)
    {
        if (_transactionCommitted)
        {
            throw new TransactionAlreadyCommittedException();
        }
        if (_transaction is null)
        {
            throw new NoTransactionException();
        }

        await _transaction.CommitAsync(cancellationToken);
        _transactionCommitted = true;
    }

    public async Task Rollback(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_transaction is not null && !_transactionCommitted)
        {
            _logger.LogWarning("Transaction was not committed and will be rolled back. " +
                "Call Commit() explicitly to persist changes.");
            _transaction.Rollback();
        }

        _transaction?.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_transaction is not null && !_transactionCommitted)
        {
            _logger.LogWarning("Transaction was not committed and will be rolled back. " +
                "Call Commit() explicitly to persist changes.");
            await _transaction.RollbackAsync();
        }

        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }
        _disposed = true;
    }

    private async Task EnsureTransactionStarted(CancellationToken cancellationToken)
    {
        _transaction ??= await _context.Database.BeginTransactionAsync(cancellationToken);
    }
}
