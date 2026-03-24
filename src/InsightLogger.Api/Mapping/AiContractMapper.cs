using InsightLogger.Application.Ai.DTOs;
using InsightLogger.Contracts.Ai;

namespace InsightLogger.Api.Mapping;

public static class AiContractMapper
{
    public static GetAiHealthResponse ToContract(AiHealthSummaryDto dto)
    {
        return new GetAiHealthResponse(
            Enabled: dto.Enabled,
            Providers: dto.Providers
                .Select(static provider => new AiProviderHealthItemContract(
                    Name: provider.Name,
                    Status: provider.Status,
                    DefaultModel: provider.DefaultModel,
                    Reason: provider.Reason))
                .ToArray());
    }

    public static GetAiProvidersResponse ToContract(AiProviderCatalogResultDto dto)
    {
        return new GetAiProvidersResponse(
            Items: dto.Items
                .Select(static item => new AiProviderItemContract(
                    Name: item.Name,
                    Type: item.Type,
                    Enabled: item.Enabled,
                    DefaultModel: item.DefaultModel,
                    Capabilities: new AiProviderCapabilitiesContract(
                        SupportsStreaming: item.Capabilities.SupportsStreaming,
                        SupportsToolCalling: item.Capabilities.SupportsToolCalling,
                        SupportsJsonMode: item.Capabilities.SupportsJsonMode,
                        SupportsOpenAiCompatibility: item.Capabilities.SupportsOpenAiCompatibility)))
                .ToArray());
    }
}
