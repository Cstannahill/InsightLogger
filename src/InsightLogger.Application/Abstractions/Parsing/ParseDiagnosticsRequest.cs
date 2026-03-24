using System.Collections.Generic;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Abstractions.Parsing;

public sealed record ParseDiagnosticsRequest(
    string Content,
    InputType InputType = InputType.BuildLog,
    ToolKind? ToolHint = null,
    bool CaptureUnparsedSegments = true,
    IReadOnlyDictionary<string, string>? Metadata = null);
