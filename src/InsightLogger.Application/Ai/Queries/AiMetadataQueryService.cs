using InsightLogger.Application.Abstractions.Ai;
using InsightLogger.Application.Ai.DTOs;

namespace InsightLogger.Application.Ai.Queries;

public sealed class AiMetadataQueryService : IAiMetadataQueryService
{
    private readonly IAiProviderCatalog _providerCatalog;
    private readonly IAiProviderHealthService _providerHealthService;

    public AiMetadataQueryService(
        IAiProviderCatalog providerCatalog,
        IAiProviderHealthService providerHealthService)
    {
        _providerCatalog = providerCatalog;
        _providerHealthService = providerHealthService;
    }

    public async Task<AiHealthSummaryDto> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var enabled = await _providerCatalog.IsEnabledAsync(cancellationToken);
        var providerHealth = await _providerHealthService.GetProviderHealthAsync(cancellationToken);

        return new AiHealthSummaryDto(
            Enabled: enabled,
            Providers: providerHealth
                .Select(static status => new AiProviderHealthDto(
                    Name: status.Name,
                    Status: status.Status,
                    DefaultModel: status.DefaultModel,
                    Reason: status.Reason))
                .ToArray());
    }

    public async Task<AiProviderCatalogResultDto> GetProvidersAsync(CancellationToken cancellationToken = default)
    {
        var providers = await _providerCatalog.GetProvidersAsync(cancellationToken);

        return new AiProviderCatalogResultDto(
            Items: providers
                .Select(static provider => new AiProviderDto(
                    Name: provider.Name,
                    Type: provider.Type,
                    Enabled: provider.Enabled,
                    DefaultModel: provider.DefaultModel,
                    Capabilities: new AiProviderCapabilitiesDto(
                        SupportsStreaming: provider.Capabilities.SupportsStreaming,
                        SupportsToolCalling: provider.Capabilities.SupportsToolCalling,
                        SupportsJsonMode: provider.Capabilities.SupportsJsonMode,
                        SupportsOpenAiCompatibility: provider.Capabilities.SupportsOpenAiCompatibility,
                        IsLocal: provider.Capabilities.IsLocal)))
                .ToArray());
    }
}
