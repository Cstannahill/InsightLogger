namespace InsightLogger.Contracts.Rules;

public sealed record GetRuleResponse(
    string Id,
    string Name,
    string? Description,
    int Priority,
    bool IsEnabled,
    RuleConditionContract Conditions,
    RuleActionContract Actions,
    IReadOnlyList<string> Tags,
    int MatchCount,
    DateTimeOffset? LastMatchedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
