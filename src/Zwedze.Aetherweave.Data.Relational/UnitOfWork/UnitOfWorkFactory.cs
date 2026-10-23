using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Zwedze.Aetherweave.Data.Relational.UnitOfWork;

internal sealed class UnitOfWorkFactory(DbContext dbContext, ILoggerFactory loggerFactory) : IUnitOfWorkFactory
{
    public ITransactionalUnitOfWork CreateTransactional()
    {
        var logger = loggerFactory.CreateLogger<TransactionalUnitOfWork>();
        return new TransactionalUnitOfWork(dbContext, logger);
    }
}
