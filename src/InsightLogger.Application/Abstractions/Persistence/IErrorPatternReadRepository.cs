using InsightLogger.Application.Diagnostics.DTOs;
using InsightLogger.Application.Knowledge.DTOs;
using InsightLogger.Application.Patterns.DTOs;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Abstractions.Persistence;

public interface IErrorPatternReadRepository
{
    Task<ErrorFingerprintDetailsDto?> GetByFingerprintAsync(
        string fingerprint,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnownPatternReferenceDto>> GetReferenceSummariesByFingerprintsAsync(
        IReadOnlyCollection<string> fingerprints,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopPatternItemDto>> GetTopPatternsAsync(
        ToolKind? toolKind,
        int limit,
        CancellationToken cancellationToken = default);
}
