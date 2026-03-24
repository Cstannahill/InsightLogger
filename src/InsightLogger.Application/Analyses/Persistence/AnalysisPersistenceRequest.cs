using System;
using System.Collections.Generic;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Analyses.Persistence;

public sealed record AnalysisPersistenceRequest(
    string AnalysisId,
    InputType InputType,
    ToolKind ToolDetected,
    AnalysisSummary Summary,
    IReadOnlyList<DiagnosticRecord> Diagnostics,
    IReadOnlyList<DiagnosticGroup> Groups,
    IReadOnlyList<RootCauseCandidate> RootCauseCandidates,
    ProcessingMetadata Processing,
    IReadOnlyDictionary<string, string>? Context,
    string RawContentHash,
    string? RawContent,
    DateTimeOffset CreatedAtUtc);
