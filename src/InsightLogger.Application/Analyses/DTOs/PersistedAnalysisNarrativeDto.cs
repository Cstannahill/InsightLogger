using System;
using System.Collections.Generic;
using InsightLogger.Application.Abstractions.Knowledge;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Analyses.DTOs;

public sealed record PersistedAnalysisNarrativeDto(
    string AnalysisId,
    InputType InputType,
    ToolKind ToolDetected,
    DateTimeOffset CreatedAtUtc,
    AnalysisSummary Summary,
    AnalysisNarrative Narrative,
    string? ProjectName,
    string? Repository,
    IReadOnlyList<KnowledgeReference>? KnowledgeReferences = null);
