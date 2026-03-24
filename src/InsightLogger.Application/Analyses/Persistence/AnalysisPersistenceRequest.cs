using System;
using System.Collections.Generic;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;

namespace InsightLogger.Application.Analyses.Persistence;

public sealed record AnalysisPersistenceRequest(
    string AnalysisId,
    InputType InputType,
    ToolKind ToolDetected,
    AnalysisSummary Summary,
    IReadOnlyList<DiagnosticRecord> Diagnostics,
    IReadOnlyList<DiagnosticGroup> Groups,
    IReadOnlyList<RootCauseCandidate> RootCauseCandidates,
    IReadOnlyList<RuleMatch> MatchedRules,
    AnalysisNarrative? Narrative,
    ProcessingMetadata Processing,
    IReadOnlyList<string> Warnings,
    IReadOnlyDictionary<string, string>? Context,
    string? ProjectName,
    string? Repository,
    string RawContentHash,
    string? RawContent,
    bool RawContentRedacted,
    DateTimeOffset CreatedAtUtc);
