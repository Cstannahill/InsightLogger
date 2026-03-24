using System;
using System.Threading;
using System.Threading.Tasks;

namespace InsightLogger.Application.Abstractions.Persistence;

public interface IInsightLoggerUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default);
}
