using JetBrains.Annotations;

namespace Zwedze.Aetherweave.Data;

/// <summary>
///     Creates units of work. Every write goes through <see cref="CreateTransactional" />;
///     reads need no unit of work.
/// </summary>
[UsedImplicitly]
public interface IUnitOfWorkFactory
{
    /// <summary>
    ///     Creates a new transactional unit of work bound to the current DI scope.
    /// </summary>
    ITransactionalUnitOfWork CreateTransactional();
}
