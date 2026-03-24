using System.Collections.Generic;
using InsightLogger.Contracts.Common;

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
    IReadOnlyList<KnowledgeReferenceContract> KnowledgeReferences,
    IReadOnlyList<string>? Warnings = null);
