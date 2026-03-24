namespace InsightLogger.Contracts.Rules;

public sealed record UpdateRuleRequest(
    string? Name,
    string? Description,
    int Priority,
    bool IsEnabled,
    RuleConditionContract? Conditions,
    RuleActionContract? Actions,
    IReadOnlyList<string>? Tags);
