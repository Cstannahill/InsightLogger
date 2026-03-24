using System;
using System.Collections.Generic;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Knowledge;
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
        AnalysisNarrative? narrative = null,
        ProcessingMetadata? processing = null,
        string? analysisId = null,
        IReadOnlyList<string>? warnings = null,
        IReadOnlyList<KnowledgeReference>? knowledgeReferences = null)
    {
        AnalysisId = string.IsNullOrWhiteSpace(analysisId) ? $"anl_{Guid.NewGuid():N}" : analysisId.Trim();
        InputType = inputType;
        ToolDetected = toolDetected;
        Summary = summary;
        Diagnostics = diagnostics;
        Groups = groups;
        RootCauseCandidates = rootCauseCandidates;
        MatchedRules = matchedRules ?? Array.Empty<RuleMatch>();
        Narrative = narrative;
        Processing = processing ?? new ProcessingMetadata(false, 0, null, null, 0d, 0d, 0, null, null);
        Warnings = warnings ?? Array.Empty<string>();
        KnowledgeReferences = knowledgeReferences ?? Array.Empty<KnowledgeReference>();
    }

    public string AnalysisId { get; }
    public InputType InputType { get; }
    public ToolKind ToolDetected { get; }
    public AnalysisSummary Summary { get; }
    public IReadOnlyList<DiagnosticRecord> Diagnostics { get; }
    public IReadOnlyList<DiagnosticGroup> Groups { get; }
    public IReadOnlyList<RootCauseCandidate> RootCauseCandidates { get; }
    public IReadOnlyList<RuleMatch> MatchedRules { get; }
    public AnalysisNarrative? Narrative { get; }
    public ProcessingMetadata Processing { get; }
    public IReadOnlyList<string> Warnings { get; }
    public IReadOnlyList<KnowledgeReference> KnowledgeReferences { get; }

    public AnalysisResult WithProcessing(ProcessingMetadata processing) =>
        new(
            inputType: InputType,
            toolDetected: ToolDetected,
            summary: Summary,
            diagnostics: Diagnostics,
            groups: Groups,
            rootCauseCandidates: RootCauseCandidates,
            matchedRules: MatchedRules,
            narrative: Narrative,
            processing: processing,
            analysisId: AnalysisId,
            warnings: Warnings,
            knowledgeReferences: KnowledgeReferences);

    public AnalysisResult WithEnrichedCandidates(
        IReadOnlyList<RootCauseCandidate> rootCauseCandidates,
        ProcessingMetadata processing,
        IReadOnlyList<string>? warnings = null)
        => new(
            inputType: InputType,
            toolDetected: ToolDetected,
            summary: Summary,
            diagnostics: Diagnostics,
            groups: Groups,
            rootCauseCandidates: rootCauseCandidates,
            matchedRules: MatchedRules,
            narrative: Narrative,
            processing: processing,
            analysisId: AnalysisId,
            warnings: warnings ?? Warnings,
            knowledgeReferences: KnowledgeReferences);

    public AnalysisResult WithNarrative(
        AnalysisNarrative? narrative,
        ProcessingMetadata processing,
        IReadOnlyList<string>? warnings = null)
        => new(
            inputType: InputType,
            toolDetected: ToolDetected,
            summary: Summary,
            diagnostics: Diagnostics,
            groups: Groups,
            rootCauseCandidates: RootCauseCandidates,
            matchedRules: MatchedRules,
            narrative: narrative,
            processing: processing,
            analysisId: AnalysisId,
            warnings: warnings ?? Warnings,
            knowledgeReferences: KnowledgeReferences);
}
