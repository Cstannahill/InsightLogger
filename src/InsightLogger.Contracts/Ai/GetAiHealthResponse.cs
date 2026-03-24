namespace InsightLogger.Contracts.Ai;

public sealed record GetAiHealthResponse(
    bool Enabled,
    IReadOnlyList<AiProviderHealthItemContract> Providers);
