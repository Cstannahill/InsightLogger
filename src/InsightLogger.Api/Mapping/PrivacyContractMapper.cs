using InsightLogger.Application.Privacy.DTOs;
using InsightLogger.Contracts.Privacy;

namespace InsightLogger.Api.Mapping;

public static class PrivacyContractMapper
{
    public static GetPrivacySettingsResponse ToContract(PrivacySettingsDto dto)
        => new(
            RawContentStorageEnabled: dto.RawContentStorageEnabled,
            RedactRawContentOnWrite: dto.RedactRawContentOnWrite,
            RawContentRetentionDays: dto.RawContentRetentionDays,
            AnalysisRetentionDays: dto.AnalysisRetentionDays);

    public static ApplyRetentionPoliciesResponse ToContract(RetentionExecutionResultDto dto)
        => new(
            AppliedAtUtc: dto.AppliedAtUtc,
            RawContentRetentionDays: dto.RawContentRetentionDays,
            AnalysisRetentionDays: dto.AnalysisRetentionDays,
            RawContentCutoffUtc: dto.RawContentCutoffUtc,
            AnalysisCutoffUtc: dto.AnalysisCutoffUtc,
            RawContentPurgedCount: dto.RawContentPurgedCount,
            AnalysesDeletedCount: dto.AnalysesDeletedCount);
}
