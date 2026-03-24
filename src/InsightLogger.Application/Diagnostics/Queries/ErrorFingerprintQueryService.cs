using System;
using System.Linq;
using InsightLogger.Application.Abstractions.Knowledge;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Diagnostics.DTOs;
using InsightLogger.Application.Knowledge.Services;

namespace InsightLogger.Application.Diagnostics.Queries;

public sealed class ErrorFingerprintQueryService : IErrorFingerprintQueryService
{
    private readonly IErrorPatternReadRepository _patternRepository;
    private readonly IRuleRepository _ruleRepository;
    private readonly IKnowledgeReferenceService? _knowledgeReferenceService;

    public ErrorFingerprintQueryService(
        IErrorPatternReadRepository patternRepository,
        IRuleRepository ruleRepository,
        IKnowledgeReferenceService? knowledgeReferenceService = null)
    {
        _patternRepository = patternRepository;
        _ruleRepository = ruleRepository;
        _knowledgeReferenceService = knowledgeReferenceService;
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
        var enriched = pattern with { RelatedRules = relatedRules };

        if (_knowledgeReferenceService is null)
        {
            return enriched;
        }

        var references = await _knowledgeReferenceService.GetReferencesAsync(
            new KnowledgeReferenceRequest(
                toolKind: enriched.ToolKind,
                diagnosticCodes: string.IsNullOrWhiteSpace(enriched.DiagnosticCode)
                    ? Array.Empty<string>()
                    : [enriched.DiagnosticCode!],
                fingerprints: [enriched.Fingerprint],
                categories: [enriched.Category],
                matchedRuleIds: enriched.RelatedRules.Select(static rule => rule.Id).ToArray()),
            cancellationToken);

        return enriched with { KnowledgeReferences = references };
    }
}
