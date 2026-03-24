using InsightLogger.Application.Analyses.Persistence;
using System.Threading;
using System.Threading.Tasks;

namespace InsightLogger.Application.Abstractions.Persistence;

public interface IErrorPatternRepository
{
    Task UpsertFromAnalysisAsync(AnalysisPersistenceRequest request, CancellationToken cancellationToken = default);
}
