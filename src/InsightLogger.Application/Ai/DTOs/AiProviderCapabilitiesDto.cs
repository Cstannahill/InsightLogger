namespace InsightLogger.Application.Ai.DTOs;

public sealed record AiProviderCapabilitiesDto(
    bool SupportsStreaming,
    bool SupportsToolCalling,
    bool SupportsJsonMode,
    bool SupportsOpenAiCompatibility,
    bool IsLocal);
