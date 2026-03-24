using System.Collections.Generic;

namespace InsightLogger.Contracts.Analyses;

public sealed record AnalyzeBuildLogResponse(
    string AnalysisId,
    string ToolDetected,
    AnalysisSummaryContract Summary,
    IReadOnlyList<RootCauseCandidateContract> RootCauseCandidates,
    IReadOnlyList<DiagnosticGroupContract> Groups,
    IReadOnlyList<DiagnosticContract> Diagnostics,
    IReadOnlyList<MatchedRuleContract> MatchedRules,
    ProcessingMetadataContract? Processing);
