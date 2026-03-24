using System.Collections.Generic;

namespace InsightLogger.Application.Abstractions.Ai;

public sealed record RootCauseNarrativeRequest(
    string Tool,
    int TotalDiagnostics,
    int GroupCount,
    int ErrorCount,
    int WarningCount,
    IReadOnlyList<string> TopRootCauseTitles,
    IReadOnlyList<string> DeterministicGroupSummaries,
    IReadOnlyList<string> DeterministicNextSteps,
    string DeterministicSummary,
    IReadOnlyDictionary<string, string>? Context = null,
    string? CorrelationId = null);
