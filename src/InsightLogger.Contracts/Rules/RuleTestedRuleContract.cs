namespace InsightLogger.Contracts.Rules;

public sealed record RuleTestedRuleContract(
    string? Id,
    string Name,
    bool IsEnabled,
    int Priority,
    bool IsPersisted);
