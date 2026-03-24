namespace InsightLogger.Contracts.Health;

public sealed record TelemetryCountItemContract(
    string Name,
    long Count);
