namespace InsightLogger.Contracts.Diagnostics;

public sealed record RelatedRuleSummaryContract(
    string Id,
    string Name,
    IReadOnlyList<string> MatchedBy,
    int MatchCount,
    DateTimeOffset? LastMatchedAt,
    string? ProjectName = null,
    string? Repository = null);
