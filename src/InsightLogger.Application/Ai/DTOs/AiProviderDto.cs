namespace InsightLogger.Application.Ai.DTOs;

public sealed record AiProviderDto(
    string Name,
    string Type,
    bool Enabled,
    string? DefaultModel,
    AiProviderCapabilitiesDto Capabilities);
