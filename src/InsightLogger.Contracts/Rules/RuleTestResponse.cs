using System.Collections.Generic;
using InsightLogger.Contracts.Analyses;

namespace InsightLogger.Contracts.Rules;

public sealed record RuleTestResponse(
    bool Matched,
    RuleTestedRuleContract Rule,
    string ToolDetected,
    int DiagnosticCount,
    int GroupCount,
    IReadOnlyList<DiagnosticContract> Diagnostics,
    IReadOnlyList<DiagnosticGroupContract> Groups,
    IReadOnlyList<RootCauseCandidateContract> RootCauseCandidatesBefore,
    IReadOnlyList<RootCauseCandidateContract> RootCauseCandidatesAfter,
    IReadOnlyList<RuleTestMatchContract> Matches,
    ProcessingMetadataContract? Processing);
