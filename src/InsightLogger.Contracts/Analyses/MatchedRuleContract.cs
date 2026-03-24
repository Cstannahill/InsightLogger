using System;
using System.Collections.Generic;

namespace InsightLogger.Contracts.Analyses;

public sealed record MatchedRuleContract(
    string RuleId,
    string TargetType,
    string TargetId,
    IReadOnlyList<string> MatchedConditions,
    IReadOnlyList<string> AppliedActions,
    DateTimeOffset AppliedAt);
