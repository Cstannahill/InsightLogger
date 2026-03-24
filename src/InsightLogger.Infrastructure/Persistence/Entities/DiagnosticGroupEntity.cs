namespace InsightLogger.Infrastructure.Persistence.Entities;

public sealed class DiagnosticGroupEntity
{
    public string Id { get; set; } = null!;
    public string AnalysisId { get; set; } = null!;
    public string Fingerprint { get; set; } = null!;
    public int Count { get; set; }
    public string? GroupReason { get; set; }
    public string? PrimaryDiagnosticId { get; set; }
    public string RelatedDiagnosticIdsJson { get; set; } = "[]";
    public int OrderIndex { get; set; }

    public AnalysisEntity Analysis { get; set; } = null!;
}
