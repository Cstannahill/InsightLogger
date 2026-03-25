using System;
using System.Collections.Generic;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Knowledge.DTOs;

public sealed record RelatedAnalysisReferenceDto(
    string AnalysisId,
    ToolKind ToolKind,
    DateTimeOffset CreatedAtUtc,
    string SummaryText,
    string? ProjectName,
    string? Repository,
    IReadOnlyList<string> MatchingFingerprints,
    IReadOnlyList<string>? MatchingDiagnosticCodes = null,
    string? MatchType = null,
    double? MatchScore = null);
