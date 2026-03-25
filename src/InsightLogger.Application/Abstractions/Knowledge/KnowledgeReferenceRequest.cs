using System;
using System.Collections.Generic;
using System.Linq;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Abstractions.Knowledge;

public sealed record KnowledgeReferenceRequest
{
    public KnowledgeReferenceRequest(
        ToolKind toolKind,
        IReadOnlyList<string>? diagnosticCodes = null,
        IReadOnlyList<string>? fingerprints = null,
        IReadOnlyList<string>? normalizedMessages = null,
        IReadOnlyList<DiagnosticCategory>? categories = null,
        IReadOnlyList<string>? matchedRuleIds = null,
        IReadOnlyDictionary<string, string>? context = null,
        string? analysisId = null)
    {
        ToolKind = toolKind;
        DiagnosticCodes = NormalizeStrings(diagnosticCodes);
        Fingerprints = NormalizeStrings(fingerprints);
        NormalizedMessages = NormalizeStrings(normalizedMessages);
        Categories = categories?
            .Distinct()
            .ToArray()
            ?? Array.Empty<DiagnosticCategory>();
        MatchedRuleIds = NormalizeStrings(matchedRuleIds);
        Context = context;
        AnalysisId = string.IsNullOrWhiteSpace(analysisId) ? null : analysisId.Trim();
    }

    public ToolKind ToolKind { get; }
    public IReadOnlyList<string> DiagnosticCodes { get; }
    public IReadOnlyList<string> Fingerprints { get; }
    public IReadOnlyList<string> NormalizedMessages { get; }
    public IReadOnlyList<DiagnosticCategory> Categories { get; }
    public IReadOnlyList<string> MatchedRuleIds { get; }
    public IReadOnlyDictionary<string, string>? Context { get; }
    public string? AnalysisId { get; }

    private static IReadOnlyList<string> NormalizeStrings(IReadOnlyList<string>? values)
        => values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? Array.Empty<string>();
}
