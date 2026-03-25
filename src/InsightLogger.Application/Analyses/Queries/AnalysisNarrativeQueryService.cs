using System;
using System.Linq;
using InsightLogger.Application.Abstractions.Knowledge;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Analyses.DTOs;
using InsightLogger.Application.Knowledge.Services;

namespace InsightLogger.Application.Analyses.Queries;

public sealed class AnalysisNarrativeQueryService : IAnalysisNarrativeQueryService
{
    private static readonly string[] AllowedSources = ["deterministic", "ai"];

    private readonly IAnalysisNarrativeReadRepository _repository;
    private readonly IAnalysisReadRepository? _analysisReadRepository;
    private readonly IKnowledgeReferenceService? _knowledgeReferenceService;

    public AnalysisNarrativeQueryService(
        IAnalysisNarrativeReadRepository repository,
        IAnalysisReadRepository? analysisReadRepository = null,
        IKnowledgeReferenceService? knowledgeReferenceService = null)
    {
        _repository = repository;
        _analysisReadRepository = analysisReadRepository;
        _knowledgeReferenceService = knowledgeReferenceService;
    }

    public async Task<PersistedAnalysisNarrativeDto?> GetByAnalysisIdAsync(
        GetAnalysisNarrativeQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.AnalysisId);

        var dto = await _repository.GetByAnalysisIdAsync(query.AnalysisId.Trim(), cancellationToken);
        if (dto is null || _analysisReadRepository is null || _knowledgeReferenceService is null)
        {
            return dto;
        }

        var persistedAnalysis = await _analysisReadRepository.GetByAnalysisIdAsync(query.AnalysisId.Trim(), cancellationToken);
        if (persistedAnalysis is null)
        {
            return dto;
        }

        var references = await _knowledgeReferenceService.GetReferencesAsync(
            new KnowledgeReferenceRequest(
                toolKind: dto.ToolDetected,
                diagnosticCodes: persistedAnalysis.Diagnostics
                    .Select(static diagnostic => diagnostic.Code)
                    .Where(static code => !string.IsNullOrWhiteSpace(code))
                    .Select(static code => code!)
                    .Take(4)
                    .ToArray(),
                normalizedMessages: persistedAnalysis.Diagnostics
                    .Select(static diagnostic => diagnostic.NormalizedMessage)
                    .Where(static message => !string.IsNullOrWhiteSpace(message))
                    .Take(4)
                    .ToArray(),
                fingerprints: persistedAnalysis.RootCauseCandidates
                    .Select(static candidate => candidate.Fingerprint.Value)
                    .Concat(persistedAnalysis.Diagnostics
                        .Where(static diagnostic => diagnostic.Fingerprint is not null)
                        .Select(static diagnostic => diagnostic.Fingerprint!.Value.Value))
                    .Distinct(StringComparer.Ordinal)
                    .Take(6)
                    .ToArray(),
                categories: persistedAnalysis.Diagnostics
                    .Select(static diagnostic => diagnostic.Category)
                    .Distinct()
                    .ToArray(),
                matchedRuleIds: persistedAnalysis.MatchedRules.Select(static match => match.RuleId).ToArray(),
                context: persistedAnalysis.Context,
                analysisId: dto.AnalysisId),
            cancellationToken);

        return dto with { KnowledgeReferences = references };
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
