namespace InsightLogger.Contracts.Analyses;

public sealed record GetAnalysisNarrativeResponse(
    string AnalysisId,
    string InputType,
    string ToolDetected,
    DateTimeOffset CreatedAtUtc,
    AnalysisSummaryContract Summary,
    AnalysisNarrativeContract Narrative,
    string? ProjectName,
    string? Repository);
