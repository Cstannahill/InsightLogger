namespace InsightLogger.Contracts.Ai;

public sealed record AiProviderItemContract(
    string Name,
    string Type,
    bool Enabled,
    string? DefaultModel,
    AiProviderCapabilitiesContract Capabilities);
