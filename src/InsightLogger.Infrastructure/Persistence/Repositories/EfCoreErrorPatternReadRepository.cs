using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Diagnostics.DTOs;
using InsightLogger.Application.Patterns.DTOs;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace InsightLogger.Infrastructure.Persistence.Repositories;

public sealed class EfCoreErrorPatternReadRepository : IErrorPatternReadRepository
{
    private readonly InsightLoggerDbContext _dbContext;

    public EfCoreErrorPatternReadRepository(InsightLoggerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ErrorFingerprintDetailsDto?> GetByFingerprintAsync(
        string fingerprint,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ErrorPatterns
            .AsNoTracking()
            .SingleOrDefaultAsync(pattern => pattern.Fingerprint == fingerprint, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return new ErrorFingerprintDetailsDto(
            Fingerprint: entity.Fingerprint,
            Title: entity.Title ?? "Observed recurring diagnostic pattern",
            ToolKind: ParseToolKind(entity.ToolKind),
            Category: ParseCategory(entity.Category),
            CanonicalMessage: entity.CanonicalMessage,
            OccurrenceCount: entity.OccurrenceCount,
            FirstSeenAtUtc: entity.FirstSeenAtUtc,
            LastSeenAtUtc: entity.LastSeenAtUtc,
            KnownFixes: string.IsNullOrWhiteSpace(entity.LastSuggestedFix)
                ? Array.Empty<string>()
                : new[] { entity.LastSuggestedFix! },
            RelatedRules: Array.Empty<RelatedRuleSummaryDto>());
    }

    public async Task<IReadOnlyList<TopPatternItemDto>> GetTopPatternsAsync(
        ToolKind? toolKind,
        int limit,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Infrastructure.Persistence.Entities.ErrorPatternEntity> query = _dbContext.ErrorPatterns.AsNoTracking();

        if (toolKind is not null)
        {
            var toolValue = toolKind.Value.ToString();
            query = query.Where(pattern => pattern.ToolKind == toolValue);
        }

        // SQLite cannot translate DateTimeOffset ORDER BY expressions reliably.
        // Keep the primary ordering in SQL and apply the tie-breaker on the client.
        var bufferSize = Math.Max(limit, 1) * 4;

        var patterns = await query
            .OrderByDescending(pattern => pattern.OccurrenceCount)
            .Take(bufferSize)
            .ToListAsync(cancellationToken);

        return patterns
            .OrderByDescending(pattern => pattern.OccurrenceCount)
            .ThenByDescending(pattern => pattern.LastSeenAtUtc)
            .Take(limit)
            .Select(pattern => new TopPatternItemDto(
                pattern.Fingerprint,
                pattern.Title ?? "Observed recurring diagnostic pattern",
                ParseToolKind(pattern.ToolKind),
                ParseCategory(pattern.Category),
                pattern.OccurrenceCount,
                pattern.LastSeenAtUtc))
            .ToList();
    }

    private static ToolKind ParseToolKind(string? value)
    {
        return Enum.TryParse<ToolKind>(value, ignoreCase: true, out var parsed)
            ? parsed
            : ToolKind.Unknown;
    }

    private static DiagnosticCategory ParseCategory(string? value)
    {
        return Enum.TryParse<DiagnosticCategory>(value, ignoreCase: true, out var parsed)
            ? parsed
            : DiagnosticCategory.Unknown;
    }
}
