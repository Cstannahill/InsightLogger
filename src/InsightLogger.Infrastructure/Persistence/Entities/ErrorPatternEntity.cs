using System.Collections.Generic;

namespace InsightLogger.Infrastructure.Persistence.Entities;

public sealed class ErrorPatternEntity
{
    public string Fingerprint { get; set; } = null!;
    public string? Title { get; set; }
    public string CanonicalMessage { get; set; } = null!;
    public string ToolKind { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string? DiagnosticCode { get; set; }
    public DateTimeOffset FirstSeenAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }
    public int OccurrenceCount { get; set; }
    public string? LastSuggestedFix { get; set; }

    public ICollection<PatternOccurrenceEntity> Occurrences { get; set; } = new List<PatternOccurrenceEntity>();
}
