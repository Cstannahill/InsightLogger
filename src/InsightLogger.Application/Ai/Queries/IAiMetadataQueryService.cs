using InsightLogger.Application.Ai.DTOs;

namespace InsightLogger.Application.Ai.Queries;

public interface IAiMetadataQueryService
{
    Task<AiHealthSummaryDto> GetHealthAsync(CancellationToken cancellationToken = default);

    Task<AiProviderCatalogResultDto> GetProvidersAsync(CancellationToken cancellationToken = default);
}
