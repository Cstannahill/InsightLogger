namespace InsightLogger.Contracts.Rules;

public sealed record RuleConditionContract(
    string? Tool,
    string? Code,
    string? Severity,
    string? Category,
    string? MessageRegex,
    string? FilePathRegex,
    string? Fingerprint,
    string? ProjectName = null,
    string? Repository = null);
