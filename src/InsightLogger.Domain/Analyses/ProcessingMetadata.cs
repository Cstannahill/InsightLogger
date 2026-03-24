namespace InsightLogger.Domain.Analyses;

public sealed record ProcessingMetadata(
    bool UsedAi,
    int DurationMs,
    string? Parser = null,
    string? CorrelationId = null,
    double ToolDetectionConfidence = 0,
    double ParseConfidence = 0,
    int UnparsedSegmentCount = 0,
    string? Notes = null);
