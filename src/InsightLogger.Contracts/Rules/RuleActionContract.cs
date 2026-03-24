namespace InsightLogger.Contracts.Rules;

public sealed record RuleActionContract(
    string? Title,
    string? Explanation,
    IReadOnlyList<string>? SuggestedFixes,
    double? ConfidenceAdjustment,
    bool? MarkAsPrimaryCause);
