using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Zwedze.Aetherweave.Data.Relational.UnitOfWork;

internal sealed class UnitOfWork(DbContext context, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private readonly DbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private bool _disposed;

    public async Task SaveChanges(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        InternalDispose();
    }

    public ValueTask DisposeAsync()
    {
        InternalDispose();
        return ValueTask.CompletedTask;
    }

    private void InternalDispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_context.ChangeTracker.HasChanges())
        {
            logger.LogWarning("Changes were not saved");
        }

        _disposed = true;
    }
}
