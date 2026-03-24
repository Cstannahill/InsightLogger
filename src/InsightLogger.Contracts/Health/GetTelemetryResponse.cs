namespace InsightLogger.Contracts.Health;

public sealed record GetTelemetryResponse(
    bool Enabled,
    string Service,
    DateTimeOffset GeneratedAtUtc,
    AnalysisTelemetrySummaryContract Analysis,
    HttpTelemetrySummaryContract Http);
