using System;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Infrastructure.Persistence.Db;

namespace InsightLogger.Infrastructure.Persistence.Repositories;

public sealed class EfCoreInsightLoggerUnitOfWork : IInsightLoggerUnitOfWork
{
    private readonly InsightLoggerDbContext _dbContext;

    public EfCoreInsightLoggerUnitOfWork(InsightLoggerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await work(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
