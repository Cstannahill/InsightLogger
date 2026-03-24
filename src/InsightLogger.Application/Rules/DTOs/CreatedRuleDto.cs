namespace InsightLogger.Application.Rules.DTOs;

public sealed record CreatedRuleDto(
    string Id,
    string Name,
    bool IsEnabled,
    int Priority,
    DateTimeOffset CreatedAtUtc);
