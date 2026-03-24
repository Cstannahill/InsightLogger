using System;
using System.Collections.Generic;
using System.Linq;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Abstractions.Rules;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;

namespace InsightLogger.Application.Rules.Services;

public sealed class RuleMatchingService
{
    private readonly IRuleRepository _ruleRepository;
    private readonly IRuleMatcher _ruleMatcher;

    public RuleMatchingService(
        IRuleRepository ruleRepository,
        IRuleMatcher ruleMatcher)
    {
        _ruleRepository = ruleRepository;
        _ruleMatcher = ruleMatcher;
    }

    public async Task<RuleEvaluationResult> EvaluateAsync(
        ToolKind toolKind,
        IReadOnlyList<DiagnosticRecord> diagnostics,
        IReadOnlyList<DiagnosticGroup> groups,
        IReadOnlyList<RootCauseCandidate> currentCandidates,
        IReadOnlyDictionary<string, string>? context = null,
        CancellationToken cancellationToken = default)
    {
        var rules = await _ruleRepository.GetEnabledRulesAsync(toolKind, cancellationToken);
        var preview = await EvaluateAsync(rules, diagnostics, groups, currentCandidates, context, cancellationToken);

        var matches = preview.Applications
            .Select(static application => new RuleMatch(
                RuleId: application.Rule.Id,
                TargetType: application.TargetType,
                TargetId: application.TargetId,
                MatchedConditions: application.MatchedConditions,
                AppliedActions: application.AppliedActions,
                AppliedAt: DateTimeOffset.UtcNow))
            .ToArray();

        return new RuleEvaluationResult(preview.RootCauseCandidates, matches, preview.Applications);
    }

    public async Task<RulePreviewEvaluationResult> EvaluateAsync(
        IReadOnlyList<Rule> rules,
        IReadOnlyList<DiagnosticRecord> diagnostics,
        IReadOnlyList<DiagnosticGroup> groups,
        IReadOnlyList<RootCauseCandidate> currentCandidates,
        IReadOnlyDictionary<string, string>? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(currentCandidates);

        if (rules.Count == 0 || diagnostics.Count == 0)
        {
            return new RulePreviewEvaluationResult(currentCandidates, Array.Empty<DTOs.RuleApplicationResult>());
        }

        var applications = await _ruleMatcher.MatchAsync(rules, diagnostics, groups, context, cancellationToken);
        if (applications.Count == 0)
        {
            return new RulePreviewEvaluationResult(currentCandidates, Array.Empty<DTOs.RuleApplicationResult>());
        }

        var updatedCandidates = ApplyRuleActions(currentCandidates, applications);
        return new RulePreviewEvaluationResult(updatedCandidates, applications);
    }

    private static IReadOnlyList<RootCauseCandidate> ApplyRuleActions(
        IReadOnlyList<RootCauseCandidate> candidates,
        IReadOnlyList<DTOs.RuleApplicationResult> applications)
    {
        if (candidates.Count == 0)
        {
            return candidates;
        }

        var results = new List<RootCauseCandidate>(candidates.Count);

        foreach (var candidate in candidates)
        {
            var applicable = applications
                .Where(application =>
                    (!string.IsNullOrWhiteSpace(candidate.DiagnosticId) &&
                     string.Equals(application.TargetType, "diagnostic", StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(application.TargetId, candidate.DiagnosticId, StringComparison.Ordinal)) ||
                    (!string.IsNullOrWhiteSpace(application.MatchedFingerprint) &&
                     string.Equals(application.MatchedFingerprint, candidate.Fingerprint.Value, StringComparison.Ordinal)))
                .OrderByDescending(application => application.Rule.Priority)
                .ToArray();

            if (applicable.Length == 0)
            {
                results.Add(candidate);
                continue;
            }

            var bestTitle = applicable
                .Select(static application => application.Rule.Action.Title)
                .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

            var bestExplanation = applicable
                .Select(static application => application.Rule.Action.Explanation)
                .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

            var allFixes = candidate.SuggestedFixes
                .Concat(applicable.SelectMany(static application => application.Rule.Action.SuggestedFixesOrEmpty))
                .Where(static fix => !string.IsNullOrWhiteSpace(fix))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var confidenceDelta = applicable.Sum(static application => application.Rule.Action.ConfidenceAdjustment);
            if (applicable.Any(static application => application.Rule.Action.MarkAsPrimaryCause))
            {
                confidenceDelta += 0.10d;
            }

            var updatedConfidence = Math.Clamp(candidate.Confidence + confidenceDelta, 0d, 1d);

            var signals = candidate.Signals
                .Concat(applicable.Select(static application => $"rule:{application.Rule.Name}"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            results.Add(candidate with
            {
                Title = bestTitle ?? candidate.Title,
                Explanation = bestExplanation ?? candidate.Explanation,
                Confidence = updatedConfidence,
                SuggestedFixes = allFixes,
                Signals = signals
            });
        }

        return results
            .OrderByDescending(static candidate => candidate.Confidence)
            .ToArray();
    }
}
