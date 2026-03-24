namespace InsightLogger.Domain.Analyses;

public sealed record AnalysisSummary(
    int TotalDiagnostics,
    int GroupCount,
    int PrimaryIssueCount,
    int ErrorCount,
    int WarningCount);
