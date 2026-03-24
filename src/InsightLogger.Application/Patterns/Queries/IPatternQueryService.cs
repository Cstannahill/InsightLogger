using InsightLogger.Application.Patterns.DTOs;

namespace InsightLogger.Application.Patterns.Queries;

public interface IPatternQueryService
{
    Task<IReadOnlyList<TopPatternItemDto>> GetTopPatternsAsync(
        GetTopPatternsQuery query,
        CancellationToken cancellationToken = default);
}
