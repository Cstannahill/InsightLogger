namespace InsightLogger.Contracts.Analyses;

public sealed record AnalysisNarrativeHistoryItemContract(
    string AnalysisId,
    string ToolDetected,
    DateTimeOffset CreatedAtUtc,
    AnalysisSummaryContract Summary,
    string SummaryText,
    string Source,
    string? Provider,
    string? Model,
    string? Status,
    bool FallbackUsed,
    string? ProjectName,
    string? Repository,
    IReadOnlyList<string> MatchedFields,
    string? MatchSnippet);
