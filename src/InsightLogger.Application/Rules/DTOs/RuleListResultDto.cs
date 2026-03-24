namespace InsightLogger.Application.Rules.DTOs;

public sealed record RuleListResultDto(
    IReadOnlyList<RuleListItemDto> Items,
    int Total);
