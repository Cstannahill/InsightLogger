namespace InsightLogger.Application.Rules.DTOs;

public sealed record RuleEnabledStateDto(
    string Id,
    bool IsEnabled,
    DateTimeOffset UpdatedAtUtc);
