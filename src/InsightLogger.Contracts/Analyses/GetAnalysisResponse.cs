using System;
using System.Collections.Generic;
using InsightLogger.Contracts.Common;

namespace InsightLogger.Contracts.Analyses;

public sealed record GetAnalysisResponse(
    string AnalysisId,
    string InputType,
    string ToolDetected,
    DateTimeOffset CreatedAtUtc,
    AnalysisSummaryContract Summary,
    IReadOnlyList<RootCauseCandidateContract> RootCauseCandidates,
    IReadOnlyList<DiagnosticGroupContract> Groups,
    IReadOnlyList<DiagnosticContract> Diagnostics,
    IReadOnlyList<MatchedRuleContract> MatchedRules,
    AnalysisNarrativeContract? Narrative,
    ProcessingMetadataContract Processing,
    IReadOnlyList<string> Warnings,
    IReadOnlyDictionary<string, string>? Context,
    string? ProjectName,
    string? Repository,
    string RawContentHash,
    bool RawContentStored,
    bool RawContentRedacted,
    string? RawContent,
    IReadOnlyList<KnowledgeReferenceContract> KnowledgeReferences);
