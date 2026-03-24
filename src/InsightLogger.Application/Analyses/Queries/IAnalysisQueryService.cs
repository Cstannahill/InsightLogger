using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Analyses.DTOs;

namespace InsightLogger.Application.Analyses.Queries;

public interface IAnalysisQueryService
{
    Task<PersistedAnalysisDto?> GetByAnalysisIdAsync(
        GetAnalysisByIdQuery query,
        CancellationToken cancellationToken = default);
}
