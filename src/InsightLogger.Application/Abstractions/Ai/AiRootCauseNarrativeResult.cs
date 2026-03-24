using System;
using System.Collections.Generic;
using System.Linq;

namespace InsightLogger.Application.Abstractions.Ai;

public sealed record AiRootCauseNarrativeResult(
    bool Success,
    string? Summary,
    IReadOnlyList<string> GroupSummaries,
    IReadOnlyList<string> RecommendedNextSteps,
    string? Provider,
    string? Model,
    string Status,
    bool FallbackUsed,
    string? Reason)
{
    public static AiRootCauseNarrativeResult Successful(
        string summary,
        IReadOnlyList<string>? groupSummaries,
        IReadOnlyList<string>? recommendedNextSteps,
        string provider,
        string model,
        bool fallbackUsed = false)
        => new(
            Success: true,
            Summary: Normalize(summary),
            GroupSummaries: NormalizeItems(groupSummaries),
            RecommendedNextSteps: NormalizeItems(recommendedNextSteps),
            Provider: provider,
            Model: model,
            Status: "completed",
            FallbackUsed: fallbackUsed,
            Reason: null);

    public static AiRootCauseNarrativeResult Failure(
        string status,
        string? reason,
        string? provider = null,
        string? model = null,
        bool fallbackUsed = false)
        => new(
            Success: false,
            Summary: null,
            GroupSummaries: Array.Empty<string>(),
            RecommendedNextSteps: Array.Empty<string>(),
            Provider: provider,
            Model: model,
            Status: status,
            FallbackUsed: fallbackUsed,
            Reason: reason);

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

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedLines = value
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return string.Join(" ", normalizedLines).Trim();
    }
}
