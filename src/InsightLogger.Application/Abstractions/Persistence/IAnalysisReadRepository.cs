using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Analyses.DTOs;
using InsightLogger.Application.Knowledge.DTOs;

namespace InsightLogger.Application.Abstractions.Persistence;

public interface IAnalysisReadRepository
{
    Task<PersistedAnalysisDto?> GetByAnalysisIdAsync(
        string analysisId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RelatedAnalysisReferenceDto>> GetRecentRelatedAnalysesAsync(
        IReadOnlyCollection<string> fingerprints,
        string? excludeAnalysisId,
        string? projectName,
        string? repository,
        int limit,
        CancellationToken cancellationToken = default);
}
