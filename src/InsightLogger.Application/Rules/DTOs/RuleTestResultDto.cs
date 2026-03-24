using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Rules.DTOs;

public sealed record RuleTestResultDto(
    string? RuleId,
    string RuleName,
    bool IsEnabled,
    int Priority,
    bool IsPersisted,
    ToolKind ToolDetected,
    IReadOnlyList<DiagnosticRecord> Diagnostics,
    IReadOnlyList<DiagnosticGroup> Groups,
    IReadOnlyList<RootCauseCandidate> RootCauseCandidatesBefore,
    IReadOnlyList<RootCauseCandidate> RootCauseCandidatesAfter,
    IReadOnlyList<RuleTestMatchDto> Matches,
    ProcessingMetadata Processing);
