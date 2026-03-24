namespace InsightLogger.Contracts.Rules;

public sealed record GetRulesResponse(
    IReadOnlyList<RuleListItemContract> Items,
    int Total);
