using System;
using System.Collections.Generic;

namespace InsightLogger.Domain.Rules;

public sealed record RuleMatch(
    string RuleId,
    string TargetType,
    string TargetId,
    IReadOnlyList<string> MatchedConditions,
    IReadOnlyList<string> AppliedActions,
    DateTimeOffset AppliedAt);
