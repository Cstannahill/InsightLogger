namespace InsightLogger.Contracts.Health;

public sealed record HttpTelemetrySummaryContract(
    long TotalRequests,
    double AverageDurationMs,
    IReadOnlyList<TelemetryCountItemContract> Methods,
    IReadOnlyList<TelemetryCountItemContract> StatusCodes,
    IReadOnlyList<TelemetryCountItemContract> Routes);
