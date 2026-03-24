using System.Collections.Generic;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Rules.Commands;

public sealed record TestRuleCommand(
    string Content,
    InputType InputType,
    ToolKind? ToolHint,
    string? RuleId,
    CreateRuleCommand? DraftRule,
    IReadOnlyDictionary<string, string>? Context = null);
