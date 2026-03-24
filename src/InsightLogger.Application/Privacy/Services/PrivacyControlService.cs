using System;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Abstractions.Privacy;
using InsightLogger.Application.Privacy.DTOs;

namespace InsightLogger.Application.Privacy.Services;

public sealed class PrivacyControlService : IPrivacyControlService
{
    private readonly IAnalysisPrivacyRepository _repository;
    private readonly IPrivacyPolicyProvider _policyProvider;

    public PrivacyControlService(
        IAnalysisPrivacyRepository repository,
        IPrivacyPolicyProvider policyProvider)
    {
        _repository = repository;
        _policyProvider = policyProvider;
    }

    public Task<PrivacySettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var policy = _policyProvider.GetCurrentPolicy();
        return Task.FromResult(new PrivacySettingsDto(
            RawContentStorageEnabled: policy.RawContentStorageEnabled,
            RedactRawContentOnWrite: policy.RedactRawContentOnWrite,
            RawContentRetentionDays: policy.RawContentRetentionDays,
            AnalysisRetentionDays: policy.AnalysisRetentionDays));
    }

    public Task<RetentionExecutionResultDto> ApplyRetentionAsync(CancellationToken cancellationToken = default)
    {
        var policy = _policyProvider.GetCurrentPolicy();
        return _repository.ApplyRetentionAsync(
            rawContentRetentionDays: policy.RawContentRetentionDays,
            analysisRetentionDays: policy.AnalysisRetentionDays,
            cancellationToken);
    }

    public Task<bool> PurgeRawContentAsync(string analysisId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(analysisId);
        return _repository.PurgeRawContentAsync(analysisId.Trim(), cancellationToken);
    }

    public Task<bool> DeleteAnalysisAsync(string analysisId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(analysisId);
        return _repository.DeleteAnalysisAsync(analysisId.Trim(), cancellationToken);
    }
}
