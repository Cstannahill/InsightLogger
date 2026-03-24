namespace InsightLogger.Application.Privacy.DTOs;

public sealed record PrivacySettingsDto(
    bool RawContentStorageEnabled,
    bool RedactRawContentOnWrite,
    int? RawContentRetentionDays,
    int? AnalysisRetentionDays);
