using System;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Analyses.DTOs;

namespace InsightLogger.Application.Analyses.Queries;

public sealed class AnalysisNarrativeQueryService : IAnalysisNarrativeQueryService
{
    private static readonly string[] AllowedSources = ["deterministic", "ai"];

    private readonly IAnalysisNarrativeReadRepository _repository;

    public AnalysisNarrativeQueryService(IAnalysisNarrativeReadRepository repository)
    {
        _repository = repository;
    }

    public Task<PersistedAnalysisNarrativeDto?> GetByAnalysisIdAsync(
        GetAnalysisNarrativeQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.AnalysisId);

        return _repository.GetByAnalysisIdAsync(query.AnalysisId.Trim(), cancellationToken);
    }

    public Task<IReadOnlyList<AnalysisNarrativeHistoryItemDto>> GetRecentAsync(
        GetAnalysisNarrativesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Limit is <= 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(query.Limit), "Limit must be between 1 and 100.");
        }

        var source = NormalizeSource(query.Source);
        var text = NormalizeText(query.Text);

        return _repository.GetRecentAsync(
            query.ToolKind,
            source,
            NormalizeOptional(query.ProjectName),
            NormalizeOptional(query.Repository),
            text,
            query.Limit,
            cancellationToken);
    }

    private static string? NormalizeSource(string? value)
    {
        var normalized = NormalizeOptional(value)?.ToLowerInvariant();
        if (normalized is null)
        {
            return null;
        }

        if (!AllowedSources.Contains(normalized, StringComparer.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Source must be either 'deterministic' or 'ai'.");
        }

        return normalized;
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null)
        {
            return null;
        }

        if (normalized.Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Text must be 200 characters or fewer.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
