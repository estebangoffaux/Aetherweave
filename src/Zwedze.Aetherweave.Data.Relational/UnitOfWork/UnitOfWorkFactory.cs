using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Zwedze.Aetherweave.Data.Relational.UnitOfWork;

internal sealed class UnitOfWorkFactory(DbContext dbContext, ILoggerFactory loggerFactory) : IUnitOfWorkFactory
{
    private readonly DbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public IUnitOfWork CreateNonTransactional()
    {
        var logger = loggerFactory.CreateLogger<UnitOfWork>();
        return new UnitOfWork(_dbContext, logger);
    }

    public ITransactionalUnitOfWork CreateTransactional()
    {
        var logger = loggerFactory.CreateLogger<TransactionalUnitOfWork>();
        return new TransactionalUnitOfWork(_dbContext, logger);
    }
}
