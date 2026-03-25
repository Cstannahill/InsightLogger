using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Analyses.DTOs;
using InsightLogger.Application.Knowledge.DTOs;
using InsightLogger.Domain.Diagnostics;

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

    Task<IReadOnlyList<RelatedAnalysisReferenceDto>> SearchSimilarAnalysesAsync(
        ToolKind toolKind,
        IReadOnlyCollection<string> fingerprints,
        IReadOnlyCollection<string> diagnosticCodes,
        IReadOnlyCollection<DiagnosticCategory> categories,
        IReadOnlyCollection<string> normalizedMessages,
        string? excludeAnalysisId,
        string? projectName,
        string? repository,
        int limit,
        CancellationToken cancellationToken = default);
}
