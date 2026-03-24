namespace InsightLogger.Infrastructure.Persistence.Entities;

public sealed class DiagnosticEntity
{
    public string Id { get; set; } = null!;
    public string AnalysisId { get; set; } = null!;
    public string ToolKind { get; set; } = null!;
    public string? Source { get; set; }
    public string? Code { get; set; }
    public string Severity { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string NormalizedMessage { get; set; } = null!;
    public string? FilePath { get; set; }
    public int? Line { get; set; }
    public int? Column { get; set; }
    public int? EndLine { get; set; }
    public int? EndColumn { get; set; }
    public string RawSnippet { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string? Subcategory { get; set; }
    public bool IsPrimaryCandidate { get; set; }
    public string? Fingerprint { get; set; }
    public string? MetadataJson { get; set; }
    public int OrderIndex { get; set; }

    public AnalysisEntity Analysis { get; set; } = null!;
}
