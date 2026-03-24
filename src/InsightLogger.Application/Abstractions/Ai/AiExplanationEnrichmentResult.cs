using System;
using System.Collections.Generic;
using System.Linq;

namespace InsightLogger.Application.Abstractions.Ai;

public sealed record AiExplanationEnrichmentResult(
    bool Success,
    string? Explanation,
    IReadOnlyList<string> LikelyCauses,
    IReadOnlyList<string> SuggestedFixes,
    string? Provider,
    string? Model,
    string Status,
    bool FallbackUsed,
    string? Reason
)
{
    public static AiExplanationEnrichmentResult Successful(
        string explanation,
        IReadOnlyList<string>? likelyCauses,
        IReadOnlyList<string>? suggestedFixes,
        string provider,
        string model,
        bool fallbackUsed = false
    ) =>
        new(
            Success: true,
            Explanation: explanation,
            LikelyCauses: NormalizeItems(likelyCauses),
            SuggestedFixes: NormalizeItems(suggestedFixes),
            Provider: provider,
            Model: model,
            Status: "completed",
            FallbackUsed: fallbackUsed,
            Reason: null
        );

    public static AiExplanationEnrichmentResult Failure(
        string status,
        string? reason,
        string? provider = null,
        string? model = null,
        bool fallbackUsed = false
    ) =>
        new(
            Success: false,
            Explanation: null,
            LikelyCauses: Array.Empty<string>(),
            SuggestedFixes: Array.Empty<string>(),
            Provider: provider,
            Model: model,
            Status: status,
            FallbackUsed: fallbackUsed,
            Reason: reason
        );

    private static IReadOnlyList<string> NormalizeItems(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<string>();
        }

        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
