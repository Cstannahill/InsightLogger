namespace InsightLogger.Domain.Analyses;

public sealed record AiProcessingMetadata(
    bool Requested,
    string? Provider = null,
    string? Model = null,
    string? Status = null,
    bool FallbackUsed = false,
    string? Reason = null,
    string? Feature = null);
