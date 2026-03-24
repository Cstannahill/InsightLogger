using System;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Diagnostics.DTOs;

namespace InsightLogger.Application.Diagnostics.Queries;

public sealed class ErrorFingerprintQueryService : IErrorFingerprintQueryService
{
    private readonly IErrorPatternReadRepository _patternRepository;
    private readonly IRuleRepository _ruleRepository;

    public ErrorFingerprintQueryService(
        IErrorPatternReadRepository patternRepository,
        IRuleRepository ruleRepository)
    {
        _patternRepository = patternRepository;
        _ruleRepository = ruleRepository;
    }

    public async Task<ErrorFingerprintDetailsDto?> GetByFingerprintAsync(
        GetErrorByFingerprintQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Fingerprint);

        var pattern = await _patternRepository.GetByFingerprintAsync(query.Fingerprint, cancellationToken);
        if (pattern is null)
        {
            return null;
        }

        var relatedRules = await _ruleRepository.GetRelatedRuleSummariesByFingerprintAsync(query.Fingerprint, cancellationToken);
        return pattern with { RelatedRules = relatedRules };
    }
}
