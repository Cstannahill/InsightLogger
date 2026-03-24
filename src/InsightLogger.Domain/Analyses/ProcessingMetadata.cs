using System;
using System.Collections.Generic;
using System.Linq;

namespace InsightLogger.Domain.Analyses;

public sealed record ProcessingMetadata
{
    public ProcessingMetadata(
        bool UsedAi,
        int DurationMs,
        string? Parser = null,
        string? CorrelationId = null,
        double ToolDetectionConfidence = 0,
        double ParseConfidence = 0,
        int UnparsedSegmentCount = 0,
        string? Notes = null,
        AiProcessingMetadata? Ai = null,
        IReadOnlyList<AiProcessingMetadata>? AiTasks = null)
    {
        this.UsedAi = UsedAi;
        this.DurationMs = DurationMs;
        this.Parser = Parser;
        this.CorrelationId = CorrelationId;
        this.ToolDetectionConfidence = ToolDetectionConfidence;
        this.ParseConfidence = ParseConfidence;
        this.UnparsedSegmentCount = UnparsedSegmentCount;
        this.Notes = Notes;
        this.Ai = Ai;
        this.AiTasks = AiTasks?.ToArray()
            ?? (Ai is null ? Array.Empty<AiProcessingMetadata>() : new[] { Ai! });
    }

    public bool UsedAi { get; }
    public int DurationMs { get; }
    public string? Parser { get; }
    public string? CorrelationId { get; }
    public double ToolDetectionConfidence { get; }
    public double ParseConfidence { get; }
    public int UnparsedSegmentCount { get; }
    public string? Notes { get; }
    public AiProcessingMetadata? Ai { get; }
    public IReadOnlyList<AiProcessingMetadata> AiTasks { get; }
}
