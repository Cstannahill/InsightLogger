namespace InsightLogger.Application.Diagnostics.DTOs;

public sealed record RelatedRuleSummaryDto(
    string Id,
    string Name,
    IReadOnlyList<string> MatchedBy,
    int MatchCount,
    DateTimeOffset? LastMatchedAtUtc,
    string? ProjectName = null,
    string? Repository = null);
