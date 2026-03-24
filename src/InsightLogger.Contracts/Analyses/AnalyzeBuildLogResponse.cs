using System.Collections.Generic;
using InsightLogger.Contracts.Common;

namespace InsightLogger.Contracts.Analyses;

public sealed record AnalyzeBuildLogResponse(
    string AnalysisId,
    string ToolDetected,
    AnalysisSummaryContract Summary,
    IReadOnlyList<RootCauseCandidateContract> RootCauseCandidates,
    IReadOnlyList<DiagnosticGroupContract> Groups,
    IReadOnlyList<DiagnosticContract> Diagnostics,
    IReadOnlyList<MatchedRuleContract> MatchedRules,
    AnalysisNarrativeContract? Narrative,
    ProcessingMetadataContract? Processing,
    IReadOnlyList<KnowledgeReferenceContract> KnowledgeReferences,
    IReadOnlyList<string>? Warnings = null);
