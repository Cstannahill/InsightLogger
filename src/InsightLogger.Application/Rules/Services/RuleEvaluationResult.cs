using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Rules;
using InsightLogger.Application.Rules.DTOs;

namespace InsightLogger.Application.Rules.Services;

public sealed record RuleEvaluationResult(
    IReadOnlyList<RootCauseCandidate> RootCauseCandidates,
    IReadOnlyList<RuleMatch> Matches,
    IReadOnlyList<RuleApplicationResult> Applications);
