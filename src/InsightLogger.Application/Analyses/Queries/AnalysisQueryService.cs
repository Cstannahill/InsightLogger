using System;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Analyses.DTOs;

namespace InsightLogger.Application.Analyses.Queries;

public sealed class AnalysisQueryService : IAnalysisQueryService
{
    private readonly IAnalysisReadRepository _repository;

    public AnalysisQueryService(IAnalysisReadRepository repository)
    {
        _repository = repository;
    }

    public Task<PersistedAnalysisDto?> GetByAnalysisIdAsync(
        GetAnalysisByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.AnalysisId);

        return _repository.GetByAnalysisIdAsync(query.AnalysisId.Trim(), cancellationToken);
    }
}
