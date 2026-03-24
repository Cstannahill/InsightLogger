using System;
using System.Collections.Generic;
using System.Linq;

namespace InsightLogger.Domain.Analyses;

public sealed record AnalysisNarrative(
    string Summary,
    IReadOnlyList<string> GroupSummaries,
    IReadOnlyList<string> RecommendedNextSteps,
    string Source,
    string? Provider = null,
    string? Model = null,
    string? Status = null,
    bool FallbackUsed = false,
    string? Reason = null)
{
    public static AnalysisNarrative Deterministic(
        string summary,
        IReadOnlyList<string>? groupSummaries,
        IReadOnlyList<string>? recommendedNextSteps)
        => new(
            Summary: Normalize(summary),
            GroupSummaries: NormalizeItems(groupSummaries),
            RecommendedNextSteps: NormalizeItems(recommendedNextSteps),
            Source: "deterministic",
            Status: "completed");

    public AnalysisNarrative WithAi(
        string summary,
        IReadOnlyList<string>? groupSummaries,
        IReadOnlyList<string>? recommendedNextSteps,
        string provider,
        string model,
        bool fallbackUsed,
        string? reason = null)
        => new(
            Summary: Normalize(summary),
            GroupSummaries: NormalizeItems(groupSummaries),
            RecommendedNextSteps: NormalizeItems(recommendedNextSteps),
            Source: "ai",
            Provider: provider,
            Model: model,
            Status: "completed",
            FallbackUsed: fallbackUsed,
            Reason: reason);

    private static string Normalize(string value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(
                " ",
                value.Replace("\r", string.Empty, StringComparison.Ordinal)
                    .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .Trim();

    private static IReadOnlyList<string> NormalizeItems(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<string>();
        }

        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => Normalize(value))
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
