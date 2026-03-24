using System;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Analyses.DTOs;

public sealed record AnalysisNarrativeHistoryItemDto(
    string AnalysisId,
    ToolKind ToolDetected,
    DateTimeOffset CreatedAtUtc,
    AnalysisSummary Summary,
    string SummaryText,
    string Source,
    string? Provider,
    string? Model,
    string? Status,
    bool FallbackUsed,
    string? ProjectName,
    string? Repository,
    IReadOnlyList<string> MatchedFields,
    string? MatchSnippet);
