namespace InsightLogger.Contracts.Patterns;

public sealed record GetTopPatternsResponse(
    IReadOnlyList<TopPatternItemContract> Items);
