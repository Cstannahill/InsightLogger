namespace InsightLogger.Application.Rules.DTOs;

public sealed record RuleListItemDto(
    string Id,
    string Name,
    string? Description,
    bool IsEnabled,
    int Priority,
    IReadOnlyList<string> Tags,
    int MatchCount,
    DateTimeOffset? LastMatchedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string? ProjectName = null,
    string? Repository = null);
