namespace InsightLogger.Contracts.Analyses;

public sealed record AnalyzeCompilerErrorRequest(
    string? Tool,
    string? Content,
    AnalyzeRequestContextContract? Context = null,
    AnalyzeRequestOptionsContract? Options = null);
