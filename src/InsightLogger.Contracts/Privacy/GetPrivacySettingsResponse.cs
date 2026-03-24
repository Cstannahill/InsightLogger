namespace InsightLogger.Contracts.Privacy;

public sealed record GetPrivacySettingsResponse(
    bool RawContentStorageEnabled,
    bool RedactRawContentOnWrite,
    int? RawContentRetentionDays,
    int? AnalysisRetentionDays);
