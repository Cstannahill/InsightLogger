using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Knowledge;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Infrastructure.Knowledge;

public sealed class InternalKnowledgeReferenceSource : IKnowledgeReferenceSource
{
    private readonly IRuleRepository _ruleRepository;
    private readonly IErrorPatternReadRepository _errorPatternReadRepository;
    private readonly IAnalysisReadRepository _analysisReadRepository;

    public InternalKnowledgeReferenceSource(
        IRuleRepository ruleRepository,
        IErrorPatternReadRepository errorPatternReadRepository,
        IAnalysisReadRepository analysisReadRepository)
    {
        _ruleRepository = ruleRepository;
        _errorPatternReadRepository = errorPatternReadRepository;
        _analysisReadRepository = analysisReadRepository;
    }

    public async Task<IReadOnlyList<KnowledgeReference>> GetReferencesAsync(
        KnowledgeReferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var references = new List<KnowledgeReference>();

        foreach (var ruleId in request.MatchedRuleIds.Take(3))
        {
            var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken);
            if (rule is null)
            {
                continue;
            }

            references.Add(new KnowledgeReference(
                id: $"internal:rule:{rule.Id}",
                kind: "rule",
                source: "internal",
                title: rule.Name,
                summary: string.IsNullOrWhiteSpace(rule.Description)
                    ? "Matched internal rule guidance is available for this diagnostic pattern."
                    : rule.Description!,
                resourceType: "rule",
                resourceId: rule.Id,
                tags: rule.Tags));
        }

        if (request.Fingerprints.Count > 0)
        {
            var patterns = await _errorPatternReadRepository.GetReferenceSummariesByFingerprintsAsync(request.Fingerprints, cancellationToken);
            foreach (var pattern in patterns.Take(4))
            {
                var summary = pattern.OccurrenceCount <= 1
                    ? "This diagnostic fingerprint has been observed before in persisted analysis history."
                    : $"Observed {pattern.OccurrenceCount} times. Most recent persisted occurrence was {pattern.LastSeenAtUtc:yyyy-MM-dd HH:mm} UTC.";

                if (!string.IsNullOrWhiteSpace(pattern.LastSuggestedFix))
                {
                    summary += $" Last recorded fix hint: {pattern.LastSuggestedFix}.";
                }

                references.Add(new KnowledgeReference(
                    id: $"internal:pattern:{pattern.Fingerprint}",
                    kind: "recurring-pattern",
                    source: "internal",
                    title: pattern.Title,
                    summary: summary,
                    resourceType: "error-pattern",
                    resourceId: pattern.Fingerprint,
                    tags: BuildPatternTags(pattern)));
            }

            var projectName = TryGetContextValue(request.Context, "projectName");
            var repository = TryGetContextValue(request.Context, "repository");

            var relatedAnalyses = await _analysisReadRepository.GetRecentRelatedAnalysesAsync(
                request.Fingerprints,
                request.AnalysisId,
                projectName,
                repository,
                limit: 3,
                cancellationToken: cancellationToken);

            foreach (var analysis in relatedAnalyses)
            {
                var location = string.Join(" / ", new[] { analysis.ProjectName, analysis.Repository }.Where(static value => !string.IsNullOrWhiteSpace(value)));
                var summary = string.IsNullOrWhiteSpace(location)
                    ? $"Persisted analysis from {analysis.CreatedAtUtc:yyyy-MM-dd HH:mm} UTC. {analysis.SummaryText}"
                    : $"Persisted analysis from {analysis.CreatedAtUtc:yyyy-MM-dd HH:mm} UTC for {location}. {analysis.SummaryText}";

                references.Add(new KnowledgeReference(
                    id: $"internal:analysis:{analysis.AnalysisId}",
                    kind: "prior-analysis",
                    source: "internal",
                    title: "Related persisted analysis",
                    summary: summary,
                    resourceType: "analysis",
                    resourceId: analysis.AnalysisId,
                    tags: analysis.MatchingFingerprints));
            }
        }

        return references
            .GroupBy(static item => item.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .ToArray();
    }

    private static string? TryGetContextValue(IReadOnlyDictionary<string, string>? context, string key)
        => context is not null && context.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;

    private static IReadOnlyList<string> BuildPatternTags(Application.Knowledge.DTOs.KnownPatternReferenceDto pattern)
    {
        var tags = new List<string> { pattern.ToolKind.ToString().ToLowerInvariant() };

        if (!string.IsNullOrWhiteSpace(pattern.DiagnosticCode))
        {
            tags.Add(pattern.DiagnosticCode!);
        }

        if (pattern.Category != DiagnosticCategory.Unknown)
        {
            tags.Add(pattern.Category.ToString().ToLowerInvariant());
        }

        return tags;
    }
}
