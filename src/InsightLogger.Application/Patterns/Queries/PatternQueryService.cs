using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Patterns.DTOs;

namespace InsightLogger.Application.Patterns.Queries;

public sealed class PatternQueryService : IPatternQueryService
{
    private readonly IErrorPatternReadRepository _repository;

    public PatternQueryService(IErrorPatternReadRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<TopPatternItemDto>> GetTopPatternsAsync(
        GetTopPatternsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Limit is <= 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(query.Limit), "Limit must be between 1 and 100.");
        }

        return _repository.GetTopPatternsAsync(query.ToolKind, query.Limit, cancellationToken);
    }
}
