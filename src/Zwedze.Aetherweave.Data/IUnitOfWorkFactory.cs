namespace Zwedze.Aetherweave.Data;

public interface IUnitOfWorkFactory
{
    IUnitOfWork CreateNonTransactional();
    ITransactionalUnitOfWork CreateTransactional();
}
