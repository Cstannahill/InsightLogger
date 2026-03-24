using System;
using System.Collections.Generic;
using System.Linq;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Analyses.Services;

public sealed class RootCauseRankingService
{
    public IReadOnlyList<RootCauseCandidate> Rank(
        IReadOnlyList<DiagnosticRecord> diagnostics,
        IReadOnlyList<DiagnosticGroup> groups,
        double parseConfidence)
    {
        if (diagnostics.Count == 0)
        {
            return Array.Empty<RootCauseCandidate>();
        }

        var diagnosticsById = diagnostics.ToDictionary(static d => d.Id, StringComparer.Ordinal);
        var candidates = new List<RootCauseCandidate>();

        foreach (var group in groups)
        {
            if (!diagnosticsById.TryGetValue(group.PrimaryDiagnosticId, out var primary))
            {
                continue;
            }

            var confidence = CalculateConfidence(primary, group, parseConfidence);
            var signals = BuildSignals(primary, group, parseConfidence);
            var insight = DiagnosticInsightFactory.Create(primary);

            candidates.Add(new RootCauseCandidate(
                Fingerprint: primary.Fingerprint ?? new DiagnosticFingerprint("fp_unknown"),
                Title: insight.Title,
                Explanation: insight.Explanation,
                Confidence: confidence,
                Signals: signals,
                SuggestedFixes: insight.SuggestedFixes,
                DiagnosticId: primary.Id,
                GroupId: group.Fingerprint.Value));
        }

        return candidates
            .OrderByDescending(static c => c.Confidence)
            .ThenBy(static c => c.DiagnosticId, StringComparer.Ordinal)
            .ToList();
    }

    private static double CalculateConfidence(DiagnosticRecord primary, DiagnosticGroup group, double parseConfidence)
    {
        var severityWeight = primary.Severity switch
        {
            Severity.Fatal => 0.95,
            Severity.Error => 0.85,
            Severity.Warning => 0.45,
            Severity.Info => 0.2,
            _ => 0.1
        };

        var categoryBoost = primary.Category switch
        {
            DiagnosticCategory.MissingSymbol => 0.08,
            DiagnosticCategory.Dependency => 0.08,
            DiagnosticCategory.BuildSystem => 0.06,
            DiagnosticCategory.NullableSafety => 0.03,
            _ => 0.02
        };

        var codeBoost = primary.Code switch
        {
            "CS0103" => 0.05,
            "CS0246" => 0.05,
            "CS0234" => 0.05,
            _ => 0d
        };

        var repeatBoost = Math.Min(0.08, (group.Count - 1) * 0.02);
        var parseBoost = Math.Clamp(parseConfidence * 0.1, 0d, 0.1);
        var confidence = severityWeight + categoryBoost + codeBoost + repeatBoost + parseBoost;

        return Math.Round(Math.Clamp(confidence, 0d, 0.99), 2, MidpointRounding.AwayFromZero);
    }

    private static IReadOnlyList<string> BuildSignals(DiagnosticRecord primary, DiagnosticGroup group, double parseConfidence)
    {
        var signals = new List<string>();

        if (!string.IsNullOrWhiteSpace(primary.Code))
        {
            signals.Add($"diagnostic-code:{primary.Code}");
        }

        signals.Add($"category:{ToKebabCase(primary.Category)}");
        signals.Add($"severity:{primary.Severity.ToString().ToLowerInvariant()}");

        if (group.Count > 1)
        {
            signals.Add($"group-count:{group.Count}");
        }

        signals.Add($"parse-confidence:{parseConfidence:0.00}");
        signals.Add("ranked-primary:true");

        return signals;
    }

    private static string ToKebabCase(DiagnosticCategory category) => category switch
    {
        DiagnosticCategory.MissingSymbol => "missing-symbol",
        DiagnosticCategory.TypeMismatch => "type-mismatch",
        DiagnosticCategory.NullableSafety => "nullable-safety",
        DiagnosticCategory.BuildSystem => "build-system",
        DiagnosticCategory.RuntimeEnvironment => "runtime-environment",
        DiagnosticCategory.TestFailure => "test-failure",
        _ => category.ToString().ToLowerInvariant()
    };
}
