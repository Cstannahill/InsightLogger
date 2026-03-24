namespace InsightLogger.Contracts.Rules;

public sealed record RuleTestRequest(
    string? RuleId,
    CreateRuleRequest? Rule,
    string? Tool,
    string? InputType,
    string? Content,
    string? ProjectName = null,
    string? Repository = null);
