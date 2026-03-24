namespace InsightLogger.Application.Abstractions.Telemetry;

public sealed record HttpRequestTelemetryEvent(
    string Method,
    string Route,
    int StatusCode,
    int DurationMs,
    string? CorrelationId = null);
