using System.Collections.Generic;

namespace InsightLogger.Application.Abstractions.Ai;

public sealed record ExplanationEnrichmentRequest(
    string Tool,
    string? DiagnosticCode,
    string? Category,
    string Title,
    string Explanation,
    IReadOnlyList<string> LikelyCauses,
    IReadOnlyList<string> SuggestedFixes,
    IReadOnlyList<string> Signals,
    string? NormalizedMessage = null,
    IReadOnlyDictionary<string, string>? Context = null,
    string? CorrelationId = null);
