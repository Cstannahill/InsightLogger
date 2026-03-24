namespace InsightLogger.Contracts.Health;

public sealed record AnalysisTelemetrySummaryContract(
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
    IReadOnlyList<TelemetryCountItemContract> ToolSelections,
    IReadOnlyList<TelemetryCountItemContract> ParserSelections,
    IReadOnlyList<TelemetryFingerprintItemContract> TopFingerprints);
