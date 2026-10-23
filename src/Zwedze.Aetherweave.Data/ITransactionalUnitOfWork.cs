using JetBrains.Annotations;
using Zwedze.Aetherweave.Data.Exceptions;

namespace Zwedze.Aetherweave.Data;

/// <summary>
///     Unit of work wrapping all its <see cref="IUnitOfWork.SaveChanges" /> calls in a single transaction.
///     <list type="bullet">
///         <item>The transaction starts lazily, on the first <see cref="IUnitOfWork.SaveChanges" />.</item>
///         <item><see cref="Commit" /> must be called explicitly; disposing without it rolls back and logs a warning.</item>
///         <item>
///             <see cref="Commit" /> and <see cref="Rollback" /> are terminal: afterwards every call throws
///             <see cref="TransactionAlreadyCommittedException" /> or <see cref="TransactionAlreadyRolledBackException" />.
///         </item>
///         <item>Only one unit of work may be open at a time per DI scope.</item>
///     </list>
/// </summary>
[UsedImplicitly]
public interface ITransactionalUnitOfWork : IUnitOfWork
{
    /// <summary>
    ///     Commits the transaction.
    /// </summary>
    /// <param name="ct">The cancellation token to use.</param>
    /// <exception cref="NoTransactionException">No <see cref="IUnitOfWork.SaveChanges" /> was called before.</exception>
    /// <exception cref="TransactionAlreadyCommittedException">The unit of work is already committed.</exception>
    /// <exception cref="TransactionAlreadyRolledBackException">The unit of work is already rolled back.</exception>
    Task Commit(CancellationToken ct);

    /// <summary>
    ///     Rolls the transaction back (if one was started) and discards all pending changes.
    ///     Use it for an explicit business decision; on exceptions, disposal already rolls back.
    /// </summary>
    /// <param name="ct">The cancellation token to use.</param>
    /// <exception cref="TransactionAlreadyCommittedException">The unit of work is already committed.</exception>
    /// <exception cref="TransactionAlreadyRolledBackException">The unit of work is already rolled back.</exception>
    Task Rollback(CancellationToken ct);
}
