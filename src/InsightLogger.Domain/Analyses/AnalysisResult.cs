using System;
using System.Collections.Generic;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;

namespace InsightLogger.Domain.Analyses;

public sealed class AnalysisResult
{
    public AnalysisResult(
        InputType inputType,
        ToolKind toolDetected,
        AnalysisSummary summary,
        IReadOnlyList<DiagnosticRecord> diagnostics,
        IReadOnlyList<DiagnosticGroup> groups,
        IReadOnlyList<RootCauseCandidate> rootCauseCandidates,
        IReadOnlyList<RuleMatch>? matchedRules = null,
        ProcessingMetadata? processing = null,
        string? analysisId = null)
    {
        AnalysisId = string.IsNullOrWhiteSpace(analysisId) ? $"anl_{Guid.NewGuid():N}" : analysisId.Trim();
        InputType = inputType;
        ToolDetected = toolDetected;
        Summary = summary;
        Diagnostics = diagnostics;
        Groups = groups;
        RootCauseCandidates = rootCauseCandidates;
        MatchedRules = matchedRules ?? Array.Empty<RuleMatch>();
        Processing = processing ?? new ProcessingMetadata(false, 0, null, null, 0d, 0d, 0, null);
    }

    public string AnalysisId { get; }
    public InputType InputType { get; }
    public ToolKind ToolDetected { get; }
    public AnalysisSummary Summary { get; }
    public IReadOnlyList<DiagnosticRecord> Diagnostics { get; }
    public IReadOnlyList<DiagnosticGroup> Groups { get; }
    public IReadOnlyList<RootCauseCandidate> RootCauseCandidates { get; }
    public IReadOnlyList<RuleMatch> MatchedRules { get; }
    public ProcessingMetadata Processing { get; }

    public AnalysisResult WithProcessing(ProcessingMetadata processing) =>
        new(
            inputType: InputType,
            toolDetected: ToolDetected,
            summary: Summary,
            diagnostics: Diagnostics,
            groups: Groups,
            rootCauseCandidates: RootCauseCandidates,
            matchedRules: MatchedRules,
            processing: processing,
            analysisId: AnalysisId);
}
