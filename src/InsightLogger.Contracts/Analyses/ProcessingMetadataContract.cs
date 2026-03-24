using System.Collections.Generic;

namespace InsightLogger.Contracts.Analyses;

public sealed record ProcessingMetadataContract(
    bool UsedAi,
    int DurationMs,
    string? Parser,
    string? CorrelationId,
    double ToolDetectionConfidence,
    double ParseConfidence,
    int UnparsedSegmentCount,
    string? Notes,
    AiProcessingMetadataContract? Ai,
    IReadOnlyList<AiProcessingMetadataContract> AiTasks);
