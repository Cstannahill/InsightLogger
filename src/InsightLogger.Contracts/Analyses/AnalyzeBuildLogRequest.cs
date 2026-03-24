namespace InsightLogger.Contracts.Analyses;

public sealed record AnalyzeBuildLogRequest(
    string? Tool,
    string? Content,
    string? ProjectName = null,
    string? Repository = null,
    string? Branch = null,
    string? CommitSha = null,
    AnalyzeEnvironmentContract? Environment = null,
    AnalyzeRequestOptionsContract? Options = null);
