using InsightLogger.Application.Analyses.DTOs;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Abstractions.Persistence;

public interface IAnalysisNarrativeReadRepository
{
    Task<PersistedAnalysisNarrativeDto?> GetByAnalysisIdAsync(
        string analysisId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnalysisNarrativeHistoryItemDto>> GetRecentAsync(
        ToolKind? toolKind,
        string? source,
        string? projectName,
        string? repository,
        string? text,
        int limit,
        CancellationToken cancellationToken = default);
}
