namespace InsightLogger.Application.Abstractions.Telemetry;

public sealed record AnalysisTelemetryEvent(
    string InputType,
    string ToolDetected,
    string? Parser,
    bool Succeeded,
    bool ParseSucceeded,
    bool AiRequested,
    bool AiCompleted,
    bool PersistenceRequested,
    bool PersistenceSucceeded,
    bool IsUnmatched,
    int DurationMs,
    int DiagnosticsCount,
    int GroupCount,
    int RootCauseCandidateCount,
    int UnparsedSegmentCount,
    IReadOnlyList<string> Fingerprints,
    string? CorrelationId = null,
    string? FailureReason = null);
