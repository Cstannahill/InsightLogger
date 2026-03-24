using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Domain.Rules;

public sealed record RuleCondition(
    ToolKind? ToolKind = null,
    string? Code = null,
    Severity? Severity = null,
    DiagnosticCategory? Category = null,
    string? MessageRegex = null,
    string? FilePathRegex = null,
    string? Fingerprint = null,
    string? ProjectName = null,
    string? Repository = null)
{
    public bool HasAnyTrigger() =>
        ToolKind is not null ||
        !string.IsNullOrWhiteSpace(Code) ||
        Severity is not null ||
        Category is not null ||
        !string.IsNullOrWhiteSpace(MessageRegex) ||
        !string.IsNullOrWhiteSpace(FilePathRegex) ||
        !string.IsNullOrWhiteSpace(Fingerprint) ||
        !string.IsNullOrWhiteSpace(ProjectName) ||
        !string.IsNullOrWhiteSpace(Repository);
}
