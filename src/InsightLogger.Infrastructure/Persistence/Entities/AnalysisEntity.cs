using System.Collections.Generic;

namespace InsightLogger.Infrastructure.Persistence.Entities;

public sealed class AnalysisEntity
{
    public string Id { get; set; } = null!;
    public string InputType { get; set; } = null!;
    public string ToolDetected { get; set; } = null!;
    public int TotalDiagnostics { get; set; }
    public int GroupCount { get; set; }
    public int PrimaryIssueCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public bool UsedAi { get; set; }
    public int DurationMs { get; set; }
    public string? Parser { get; set; }
    public string? CorrelationId { get; set; }
    public double ToolDetectionConfidence { get; set; }
    public double ParseConfidence { get; set; }
    public int UnparsedSegmentCount { get; set; }
    public string? Notes { get; set; }
    public string RawContentHash { get; set; } = null!;
    public string? RawContent { get; set; }
    public string? ContextJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<DiagnosticEntity> Diagnostics { get; set; } = new List<DiagnosticEntity>();
    public ICollection<DiagnosticGroupEntity> Groups { get; set; } = new List<DiagnosticGroupEntity>();
    public ICollection<PatternOccurrenceEntity> PatternOccurrences { get; set; } = new List<PatternOccurrenceEntity>();
}
