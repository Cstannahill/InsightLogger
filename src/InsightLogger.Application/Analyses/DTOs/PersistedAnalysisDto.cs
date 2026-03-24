using System;
using System.Collections.Generic;
using InsightLogger.Application.Abstractions.Knowledge;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;

namespace InsightLogger.Application.Analyses.DTOs;

public sealed record PersistedAnalysisDto(
    string AnalysisId,
    InputType InputType,
    ToolKind ToolDetected,
    DateTimeOffset CreatedAtUtc,
    AnalysisSummary Summary,
    IReadOnlyList<RootCauseCandidate> RootCauseCandidates,
    IReadOnlyList<DiagnosticGroup> Groups,
    IReadOnlyList<DiagnosticRecord> Diagnostics,
    IReadOnlyList<RuleMatch> MatchedRules,
    AnalysisNarrative? Narrative,
    ProcessingMetadata Processing,
    IReadOnlyList<string> Warnings,
    IReadOnlyDictionary<string, string>? Context,
    string? ProjectName,
    string? Repository,
    string RawContentHash,
    bool RawContentRedacted,
    string? RawContent,
    IReadOnlyList<KnowledgeReference>? KnowledgeReferences = null);
