namespace InsightLogger.Application.Ai.DTOs;

public sealed record AiProviderHealthDto(
    string Name,
    string Status,
    string? DefaultModel,
    string? Reason);
