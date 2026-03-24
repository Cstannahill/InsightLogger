namespace InsightLogger.Application.Abstractions.Ai;

public sealed record AiProviderDefinition(
    string Name,
    string Type,
    bool Enabled,
    string? DefaultModel,
    string? BaseUrl,
    bool RequiresApiKey,
    bool HasApiKey,
    AiProviderCapabilities Capabilities);
