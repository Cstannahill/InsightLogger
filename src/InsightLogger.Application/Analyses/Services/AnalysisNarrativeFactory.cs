using System;
using System.Collections.Generic;
using System.Linq;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Analyses.Services;

public sealed class AnalysisNarrativeFactory
{
    public AnalysisNarrative? Build(
        ToolKind toolKind,
        AnalysisSummary summary,
        IReadOnlyList<DiagnosticGroup> groups,
        IReadOnlyList<RootCauseCandidate> rootCauseCandidates)
    {
        if (!ShouldBuildNarrative(summary, groups, rootCauseCandidates))
        {
            return null;
        }

        var primary = rootCauseCandidates.FirstOrDefault();
        var secondary = rootCauseCandidates.Skip(1).Take(2).ToArray();

        var tool = ToFriendlyTool(toolKind);
        var summaryText = BuildSummaryText(tool, summary, primary, secondary);
        var groupSummaries = BuildGroupSummaries(groups, rootCauseCandidates);
        var nextSteps = BuildNextSteps(rootCauseCandidates);

        return AnalysisNarrative.Deterministic(summaryText, groupSummaries, nextSteps);
    }

    public static bool ShouldBuildNarrative(
        AnalysisSummary summary,
        IReadOnlyList<DiagnosticGroup> groups,
        IReadOnlyList<RootCauseCandidate> rootCauseCandidates)
        => summary.TotalDiagnostics > 1 || groups.Count > 1 || rootCauseCandidates.Count > 1;

    private static string BuildSummaryText(
        string tool,
        AnalysisSummary summary,
        RootCauseCandidate? primary,
        IReadOnlyList<RootCauseCandidate> secondary)
    {
        var builder = new List<string>
        {
            $"The {tool} log contains {summary.TotalDiagnostics} diagnostic{Pluralize(summary.TotalDiagnostics)}, grouped into {summary.GroupCount} likely issue cluster{Pluralize(summary.GroupCount)}."
        };

        if (primary is not null)
        {
            builder.Add($"The strongest starting point is {primary.Title.ToLowerInvariant()}.");
        }

        if (secondary.Count > 0)
        {
            builder.Add($"Secondary issue{Pluralize(secondary.Count)} include {JoinTitles(secondary.Select(static c => c.Title))}.");
        }

        if (summary.WarningCount > 0 && summary.ErrorCount > 0)
        {
            builder.Add("Warnings are likely downstream noise unless they block the same area as the primary error.");
        }

        return string.Join(" ", builder);
    }

    private static IReadOnlyList<string> BuildGroupSummaries(
        IReadOnlyList<DiagnosticGroup> groups,
        IReadOnlyList<RootCauseCandidate> rootCauseCandidates)
    {
        var items = new List<string>();

        foreach (var group in groups.Take(3))
        {
            var matchingCandidate = rootCauseCandidates.FirstOrDefault(candidate =>
                string.Equals(candidate.GroupId, group.Fingerprint.Value, StringComparison.Ordinal) ||
                string.Equals(candidate.Fingerprint.Value, group.Fingerprint.Value, StringComparison.Ordinal));

            var title = matchingCandidate?.Title ?? "Grouped issue";
            items.Add($"{title}: {group.Count} related diagnostic{Pluralize(group.Count)} matched fingerprint {group.Fingerprint.Value}.");
        }

        foreach (var candidate in rootCauseCandidates.Take(3))
        {
            if (items.Count >= 3)
            {
                break;
            }

            if (items.Any(item => item.Contains(candidate.Title, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            items.Add($"{candidate.Title}: confidence {candidate.Confidence:0.00} based on {string.Join(", ", candidate.Signals.Take(3))}.");
        }

        return items;
    }

    private static IReadOnlyList<string> BuildNextSteps(IReadOnlyList<RootCauseCandidate> rootCauseCandidates)
    {
        var steps = rootCauseCandidates
            .SelectMany(static candidate => candidate.SuggestedFixes)
            .Where(static fix => !string.IsNullOrWhiteSpace(fix))
            .Select(static fix => fix.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToArray();

        if (steps.Length > 0)
        {
            return steps;
        }

        return new[]
        {
            "Start with the earliest high-confidence root cause candidate.",
            "Resolve repeated fingerprint groups before chasing downstream noise.",
            "Re-run the build after the first substantive fix to confirm what remains."
        };
    }

    private static string ToFriendlyTool(ToolKind toolKind) => toolKind switch
    {
        ToolKind.DotNet => ".NET",
        ToolKind.TypeScript => "TypeScript",
        ToolKind.Npm => "npm",
        ToolKind.Vite => "Vite",
        ToolKind.Python => "Python",
        ToolKind.Generic => "generic",
        _ => "build"
    };

    private static string JoinTitles(IEnumerable<string> titles)
    {
        var items = titles
            .Where(static title => !string.IsNullOrWhiteSpace(title))
            .Select(static title => title.Trim().ToLowerInvariant())
            .Take(3)
            .ToArray();

        return items.Length switch
        {
            0 => "other follow-on issues",
            1 => items[0],
            2 => $"{items[0]} and {items[1]}",
            _ => $"{items[0]}, {items[1]}, and {items[2]}"
        };
    }

    private static string Pluralize(int count) => count == 1 ? string.Empty : "s";
}
