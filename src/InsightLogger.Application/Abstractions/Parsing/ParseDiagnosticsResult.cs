using System.Collections.Generic;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Abstractions.Parsing;

public sealed record ParseDiagnosticsResult(
    ToolKind ToolKind,
    string ParserName,
    double ParseConfidence,
    IReadOnlyList<DiagnosticRecord> Diagnostics,
    int TotalSegments,
    int ParsedSegments,
    IReadOnlyList<string>? UnparsedSegments = null);
