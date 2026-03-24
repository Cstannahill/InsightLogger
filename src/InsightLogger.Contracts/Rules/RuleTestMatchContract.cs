using System.Collections.Generic;

namespace InsightLogger.Contracts.Rules;

public sealed record RuleTestMatchContract(
    string RuleId,
    string RuleName,
    string TargetType,
    string TargetId,
    string? MatchedFingerprint,
    IReadOnlyList<string> MatchedConditions,
    IReadOnlyList<string> AppliedActions);
