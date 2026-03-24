using InsightLogger.Domain.Rules;

namespace InsightLogger.Application.Rules.DTOs;

public sealed record RuleApplicationResult(
    Rule Rule,
    string TargetType,
    string TargetId,
    string? MatchedFingerprint,
    IReadOnlyList<string> MatchedConditions,
    IReadOnlyList<string> AppliedActions);
