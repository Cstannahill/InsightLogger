using InsightLogger.Application.Analyses.Persistence;
using System.Threading;
using System.Threading.Tasks;

namespace InsightLogger.Application.Abstractions.Persistence;

public interface IAnalysisPersistenceRepository
{
    Task SaveAsync(AnalysisPersistenceRequest request, CancellationToken cancellationToken = default);
}
