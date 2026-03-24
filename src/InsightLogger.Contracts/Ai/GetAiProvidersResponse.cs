namespace InsightLogger.Contracts.Ai;

public sealed record GetAiProvidersResponse(
    IReadOnlyList<AiProviderItemContract> Items);
