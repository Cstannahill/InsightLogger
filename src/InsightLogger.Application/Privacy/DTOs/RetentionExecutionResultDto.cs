using System;

namespace InsightLogger.Application.Privacy.DTOs;

public sealed record RetentionExecutionResultDto(
    DateTimeOffset AppliedAtUtc,
    int? RawContentRetentionDays,
    int? AnalysisRetentionDays,
    DateTimeOffset? RawContentCutoffUtc,
    DateTimeOffset? AnalysisCutoffUtc,
    int RawContentPurgedCount,
    int AnalysesDeletedCount);
