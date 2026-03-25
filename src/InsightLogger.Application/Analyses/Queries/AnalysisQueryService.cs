using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Knowledge;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Analyses.DTOs;
using InsightLogger.Application.Knowledge.Services;

namespace InsightLogger.Application.Analyses.Queries;

public sealed class AnalysisQueryService : IAnalysisQueryService
{
    private readonly IAnalysisReadRepository _repository;
    private readonly IKnowledgeReferenceService? _knowledgeReferenceService;

    public AnalysisQueryService(
        IAnalysisReadRepository repository,
        IKnowledgeReferenceService? knowledgeReferenceService = null)
    {
        _repository = repository;
        _knowledgeReferenceService = knowledgeReferenceService;
    }

    public async Task<PersistedAnalysisDto?> GetByAnalysisIdAsync(
        GetAnalysisByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.AnalysisId);

        var dto = await _repository.GetByAnalysisIdAsync(query.AnalysisId.Trim(), cancellationToken);
        if (dto is null || _knowledgeReferenceService is null)
        {
            return dto;
        }

        var references = await _knowledgeReferenceService.GetReferencesAsync(
            new KnowledgeReferenceRequest(
                toolKind: dto.ToolDetected,
                diagnosticCodes: dto.Diagnostics
                    .Select(static diagnostic => diagnostic.Code)
                    .Where(static code => !string.IsNullOrWhiteSpace(code))
                    .Select(static code => code!)
                    .Take(4)
                    .ToArray(),
                normalizedMessages: dto.Diagnostics
                    .Select(static diagnostic => diagnostic.NormalizedMessage)
                    .Where(static message => !string.IsNullOrWhiteSpace(message))
                    .Take(4)
                    .ToArray(),
                fingerprints: dto.RootCauseCandidates
                    .Select(static candidate => candidate.Fingerprint.Value)
                    .Concat(dto.Diagnostics
                        .Where(static diagnostic => diagnostic.Fingerprint is not null)
                        .Select(static diagnostic => diagnostic.Fingerprint!.Value.Value))
                    .Distinct(StringComparer.Ordinal)
                    .Take(6)
                    .ToArray(),
                categories: dto.Diagnostics
                    .Select(static diagnostic => diagnostic.Category)
                    .Distinct()
                    .ToArray(),
                matchedRuleIds: dto.MatchedRules.Select(static match => match.RuleId).ToArray(),
                context: dto.Context,
                analysisId: dto.AnalysisId),
            cancellationToken);

        return dto with { KnowledgeReferences = references };
    }
}
