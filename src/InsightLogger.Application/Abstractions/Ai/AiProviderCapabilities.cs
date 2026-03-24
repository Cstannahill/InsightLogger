namespace InsightLogger.Application.Abstractions.Ai;

public sealed record AiProviderCapabilities(
    bool SupportsStreaming,
    bool SupportsToolCalling,
    bool SupportsJsonMode,
    bool SupportsOpenAiCompatibility,
    bool IsLocal
);
