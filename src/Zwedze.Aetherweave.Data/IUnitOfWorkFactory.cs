namespace Zwedze.Aetherweave.Data;

public interface IUnitOfWorkFactory
{
    ITransactionalUnitOfWork CreateTransactional();
}
