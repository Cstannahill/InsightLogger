namespace InsightLogger.Contracts.Analyses;

public sealed record AnalysisSummaryContract(
    int TotalDiagnostics,
    int GroupCount,
    int PrimaryIssueCount,
    int ErrorCount,
    int WarningCount);
