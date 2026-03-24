namespace InsightLogger.Application.Abstractions.Ai;

public sealed record AiProviderHealthStatus(
    string Name,
    string Status,
    string? DefaultModel,
    string? Reason);
