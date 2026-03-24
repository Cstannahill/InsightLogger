namespace InsightLogger.Application.Ai.DTOs;

public sealed record AiProviderCatalogResultDto(
    IReadOnlyList<AiProviderDto> Items);
