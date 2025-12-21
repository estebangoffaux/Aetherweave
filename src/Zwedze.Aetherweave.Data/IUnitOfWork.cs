namespace Zwedze.Aetherweave.Data;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task SaveChanges(CancellationToken cancellationToken = default);
}
