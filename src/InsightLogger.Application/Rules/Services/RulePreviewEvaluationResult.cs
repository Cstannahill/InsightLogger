using InsightLogger.Application.Rules.DTOs;
using InsightLogger.Domain.Analyses;

namespace InsightLogger.Application.Rules.Services;

public sealed record RulePreviewEvaluationResult(
    IReadOnlyList<RootCauseCandidate> RootCauseCandidates,
    IReadOnlyList<RuleApplicationResult> Applications);
