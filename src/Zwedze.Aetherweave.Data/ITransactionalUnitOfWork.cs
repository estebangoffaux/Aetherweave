namespace Zwedze.Aetherweave.Data;

public interface ITransactionalUnitOfWork : IUnitOfWork
{
    Task Commit(CancellationToken cancellationToken = default);
    Task Rollback(CancellationToken cancellationToken = default);
}
