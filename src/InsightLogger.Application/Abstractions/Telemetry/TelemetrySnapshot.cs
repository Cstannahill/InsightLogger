namespace InsightLogger.Application.Abstractions.Telemetry;

public sealed record TelemetrySnapshot(
    bool Enabled,
    DateTimeOffset GeneratedAtUtc,
    AnalysisTelemetrySnapshot Analysis,
    HttpTelemetrySnapshot Http);

public sealed record AnalysisTelemetrySnapshot(
    long TotalRequests,
    long Completed,
    long Failed,
    long ParseFailures,
    long AiRequested,
    long AiCompleted,
    long PersistenceFailures,
    long UnmatchedAnalyses,
    double AverageDurationMs,
    double AverageDiagnosticsPerAnalysis,
    double AiRequestRate,
    double UnmatchedAnalysisRate,
    IReadOnlyList<TelemetryCountItem> ToolSelections,
    IReadOnlyList<TelemetryCountItem> ParserSelections,
    IReadOnlyList<TelemetryFingerprintItem> TopFingerprints);

public sealed record HttpTelemetrySnapshot(
    long TotalRequests,
    double AverageDurationMs,
    IReadOnlyList<TelemetryCountItem> Methods,
    IReadOnlyList<TelemetryCountItem> StatusCodes,
    IReadOnlyList<TelemetryCountItem> Routes);

public sealed record TelemetryCountItem(string Name, long Count);

public sealed record TelemetryFingerprintItem(string Fingerprint, long Count);
