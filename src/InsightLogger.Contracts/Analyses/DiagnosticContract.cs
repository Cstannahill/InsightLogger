namespace InsightLogger.Contracts.Analyses;

public sealed record DiagnosticContract(
    string Id,
    string Tool,
    string? Code,
    string Severity,
    string Message,
    string NormalizedMessage,
    string? FilePath,
    int? Line,
    int? Column,
    int? EndLine,
    int? EndColumn,
    string Category,
    string? Subcategory,
    string? Fingerprint,
    bool IsPrimaryCandidate);
