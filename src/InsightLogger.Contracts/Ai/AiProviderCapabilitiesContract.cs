namespace InsightLogger.Contracts.Ai;

public sealed record AiProviderCapabilitiesContract(
    bool SupportsStreaming,
    bool SupportsToolCalling,
    bool SupportsJsonMode,
    bool SupportsOpenAiCompatibility);
