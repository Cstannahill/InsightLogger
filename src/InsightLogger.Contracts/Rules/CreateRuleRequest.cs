namespace InsightLogger.Contracts.Rules;

public sealed record CreateRuleRequest(
    string? Name,
    string? Description,
    int Priority,
    bool IsEnabled,
    RuleConditionContract? Conditions,
    RuleActionContract? Actions,
    IReadOnlyList<string>? Tags);
