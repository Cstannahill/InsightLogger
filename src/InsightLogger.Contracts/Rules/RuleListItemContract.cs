namespace InsightLogger.Contracts.Rules;

public sealed record RuleListItemContract(
    string Id,
    string Name,
    string? Description,
    bool IsEnabled,
    int Priority,
    IReadOnlyList<string> Tags,
    int MatchCount,
    DateTimeOffset? LastMatchedAt,
    DateTimeOffset UpdatedAt,
    string? ProjectName = null,
    string? Repository = null);
