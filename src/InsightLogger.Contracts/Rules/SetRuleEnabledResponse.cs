namespace InsightLogger.Contracts.Rules;

public sealed record SetRuleEnabledResponse(
    string Id,
    bool IsEnabled,
    DateTimeOffset UpdatedAt);
