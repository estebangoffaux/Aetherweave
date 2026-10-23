using JetBrains.Annotations;

namespace Zwedze.Aetherweave.Data;

/// <summary>
///     Flushes pending changes to the underlying store.
///     Always consume with <c>await using</c> so disposal runs even when an exception is thrown.
/// </summary>
[UsedImplicitly]
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     Writes the pending changes to the underlying store.
    /// </summary>
    /// <param name="ct">The cancellation token to use.</param>
    Task SaveChanges(CancellationToken ct);
}
