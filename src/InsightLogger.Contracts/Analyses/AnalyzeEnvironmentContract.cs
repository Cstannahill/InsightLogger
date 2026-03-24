namespace InsightLogger.Contracts.Analyses;

public sealed record AnalyzeEnvironmentContract(
    string? Os = null,
    bool? Ci = null,
    string? MachineName = null);
