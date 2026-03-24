namespace InsightLogger.Contracts.Analyses;

public sealed record GetAnalysisNarrativesResponse(
    IReadOnlyList<AnalysisNarrativeHistoryItemContract> Items);
