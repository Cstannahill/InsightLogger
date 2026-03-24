using System.Collections.Generic;

namespace InsightLogger.Domain.Rules;

public sealed record RuleAction(
    string? Title = null,
    string? Explanation = null,
    IReadOnlyList<string>? SuggestedFixes = null,
    double ConfidenceAdjustment = 0d,
    bool MarkAsPrimaryCause = false)
{
    public IReadOnlyList<string> SuggestedFixesOrEmpty => SuggestedFixes ?? System.Array.Empty<string>();

    public bool HasAnyAction() =>
        !string.IsNullOrWhiteSpace(Title) ||
        !string.IsNullOrWhiteSpace(Explanation) ||
        SuggestedFixesOrEmpty.Count > 0 ||
        ConfidenceAdjustment != 0d ||
        MarkAsPrimaryCause;
}
