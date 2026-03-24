using System.Collections.Generic;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Analyses.Commands;

public sealed record AnalyzeInputCommand(
    string Content,
    InputType InputType,
    ToolKind? ToolHint = null,
    string? CorrelationId = null,
    IReadOnlyDictionary<string, string>? Context = null,
    bool Persist = false,
    bool UseAiEnrichment = false,
    bool StoreRawContentWhenPersisting = false);
