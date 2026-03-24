namespace InsightLogger.Infrastructure.Persistence.Entities;

public sealed class PatternOccurrenceEntity
{
    public string Id { get; set; } = null!;
    public string Fingerprint { get; set; } = null!;
    public string AnalysisId { get; set; } = null!;
    public string DiagnosticId { get; set; } = null!;
    public DateTimeOffset SeenAtUtc { get; set; }

    public AnalysisEntity Analysis { get; set; } = null!;
    public ErrorPatternEntity Pattern { get; set; } = null!;
}
