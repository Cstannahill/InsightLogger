using System;

namespace InsightLogger.Application.Abstractions.Privacy;

public sealed record PrivacyPolicy(
    bool RawContentStorageEnabled,
    bool RedactRawContentOnWrite,
    int? RawContentRetentionDays,
    int? AnalysisRetentionDays)
{
    public DateTimeOffset? GetRawContentExpiresAtUtc(DateTimeOffset createdAtUtc)
        => RawContentRetentionDays.HasValue
            ? createdAtUtc.AddDays(RawContentRetentionDays.Value)
            : null;

    public DateTimeOffset? GetAnalysisExpiresAtUtc(DateTimeOffset createdAtUtc)
        => AnalysisRetentionDays.HasValue
            ? createdAtUtc.AddDays(AnalysisRetentionDays.Value)
            : null;
}
