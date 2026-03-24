using System.Collections.Generic;

namespace InsightLogger.Application.Rules.DTOs;

public sealed record RuleTestMatchDto(
    string RuleId,
    string RuleName,
    string TargetType,
    string TargetId,
    string? MatchedFingerprint,
    IReadOnlyList<string> MatchedConditions,
    IReadOnlyList<string> AppliedActions);
