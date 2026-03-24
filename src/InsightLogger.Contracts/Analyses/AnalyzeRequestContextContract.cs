namespace InsightLogger.Contracts.Analyses;

public sealed record AnalyzeRequestContextContract(
    string? ProjectName = null,
    string? Repository = null,
    string? Branch = null,
    string? CommitSha = null);
