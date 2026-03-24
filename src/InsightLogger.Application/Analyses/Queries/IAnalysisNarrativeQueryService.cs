using InsightLogger.Application.Analyses.DTOs;

namespace InsightLogger.Application.Analyses.Queries;

public interface IAnalysisNarrativeQueryService
{
    Task<PersistedAnalysisNarrativeDto?> GetByAnalysisIdAsync(
        GetAnalysisNarrativeQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnalysisNarrativeHistoryItemDto>> GetRecentAsync(
        GetAnalysisNarrativesQuery query,
        CancellationToken cancellationToken = default);
}
