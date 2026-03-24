using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Analyses.DTOs;

namespace InsightLogger.Application.Abstractions.Persistence;

public interface IAnalysisReadRepository
{
    Task<PersistedAnalysisDto?> GetByAnalysisIdAsync(
        string analysisId,
        CancellationToken cancellationToken = default);
}
