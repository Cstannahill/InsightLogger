namespace InsightLogger.Contracts.Analyses;

public sealed record AiProcessingMetadataContract(
    bool Requested,
    string? Provider,
    string? Model,
    string? Status,
    bool FallbackUsed,
    string? Reason,
    string? Feature);
