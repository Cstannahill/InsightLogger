namespace InsightLogger.Contracts.Ai;

public sealed record AiProviderHealthItemContract(
    string Name,
    string Status,
    string? DefaultModel,
    string? Reason);
