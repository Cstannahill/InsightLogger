using System.Collections.Generic;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Rules.Commands;

public sealed record CreateRuleCommand(
    string Name,
    string? Description,
    bool IsEnabled,
    int Priority,
    ToolKind? ToolKind,
    string? Code,
    Severity? Severity,
    DiagnosticCategory? Category,
    string? MessageRegex,
    string? FilePathRegex,
    string? Fingerprint,
    string? Title,
    string? Explanation,
    IReadOnlyList<string> SuggestedFixes,
    double ConfidenceAdjustment,
    bool MarkAsPrimaryCause,
    IReadOnlyList<string> Tags,
    string? ProjectName = null,
    string? Repository = null);
