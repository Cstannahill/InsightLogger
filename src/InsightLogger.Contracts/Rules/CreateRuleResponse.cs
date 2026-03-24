namespace InsightLogger.Contracts.Rules;

public sealed record CreateRuleResponse(
    string Id,
    string Name,
    bool IsEnabled,
    int Priority,
    DateTimeOffset CreatedAt);
