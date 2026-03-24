namespace InsightLogger.Contracts.Health;

public sealed record GetHealthResponse(
    string Status,
    string Service,
    string Version,
    DateTimeOffset Timestamp);
