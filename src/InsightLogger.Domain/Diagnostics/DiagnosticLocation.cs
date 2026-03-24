namespace InsightLogger.Domain.Diagnostics;

public sealed record DiagnosticLocation(
    string? FilePath,
    int? Line,
    int? Column,
    int? EndLine = null,
    int? EndColumn = null);
