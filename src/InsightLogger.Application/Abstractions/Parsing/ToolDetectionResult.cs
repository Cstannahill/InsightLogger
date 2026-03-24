using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Abstractions.Parsing;

public sealed record ToolDetectionResult(
    ToolKind ToolKind,
    double Confidence,
    string Reason,
    bool WasExplicitHint = false);
