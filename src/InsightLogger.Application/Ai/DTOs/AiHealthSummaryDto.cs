namespace InsightLogger.Application.Ai.DTOs;

public sealed record AiHealthSummaryDto(
    bool Enabled,
    IReadOnlyList<AiProviderHealthDto> Providers);
