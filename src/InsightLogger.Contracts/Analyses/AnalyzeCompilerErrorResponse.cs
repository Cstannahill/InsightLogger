using System.Collections.Generic;

namespace InsightLogger.Contracts.Analyses;

public sealed record AnalyzeCompilerErrorResponse(
    string Fingerprint,
    string ToolDetected,
    DiagnosticContract? Diagnostic,
    string Explanation,
    IReadOnlyList<string> LikelyCauses,
    IReadOnlyList<string> SuggestedFixes,
    double Confidence,
    IReadOnlyList<MatchedRuleContract> MatchedRules,
    ProcessingMetadataContract? Processing,
    IReadOnlyList<string>? Warnings = null);
