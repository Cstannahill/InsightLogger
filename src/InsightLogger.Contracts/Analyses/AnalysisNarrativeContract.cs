using System.Collections.Generic;

namespace InsightLogger.Contracts.Analyses;

public sealed record AnalysisNarrativeContract(
    string Summary,
    IReadOnlyList<string> GroupSummaries,
    IReadOnlyList<string> RecommendedNextSteps,
    string Source,
    string? Provider = null,
    string? Model = null,
    string? Status = null,
    bool FallbackUsed = false,
    string? Reason = null);
