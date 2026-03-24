using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Abstractions.Parsing;

public sealed record DiagnosticParserCoordinatorResult(
    ToolKind ToolKind,
    string? SelectedParserName,
    ParseDiagnosticsResult? ParseResult,
    string? FailureReason = null)
{
    public bool Success => ParseResult is not null;
}
