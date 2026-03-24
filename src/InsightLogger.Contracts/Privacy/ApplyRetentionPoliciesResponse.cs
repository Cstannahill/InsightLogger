using System;

namespace InsightLogger.Contracts.Privacy;

public sealed record ApplyRetentionPoliciesResponse(
    DateTimeOffset AppliedAtUtc,
    int? RawContentRetentionDays,
    int? AnalysisRetentionDays,
    DateTimeOffset? RawContentCutoffUtc,
    DateTimeOffset? AnalysisCutoffUtc,
    int RawContentPurgedCount,
    int AnalysesDeletedCount);
