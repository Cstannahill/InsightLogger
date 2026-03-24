using System.Collections.Generic;
using System.Text.RegularExpressions;
using InsightLogger.Api.Mapping;
using InsightLogger.Contracts.Common;
using InsightLogger.Contracts.Rules;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Api.Validation;

public static class RuleRequestValidator
{
    public static IReadOnlyList<ValidationErrorDetail> Validate(CreateRuleRequest? request)
        => ValidateCore(request?.Name, request?.Conditions, request?.Actions);

    public static IReadOnlyList<ValidationErrorDetail> Validate(UpdateRuleRequest? request)
        => ValidateCore(request?.Name, request?.Conditions, request?.Actions);

    public static IReadOnlyList<ValidationErrorDetail> Validate(SetRuleEnabledRequest? request)
    {
        var errors = new List<ValidationErrorDetail>();

        if (request is null)
        {
            errors.Add(new ValidationErrorDetail("request", "Request body is required."));
        }

        return errors;
    }

    public static IReadOnlyList<ValidationErrorDetail> Validate(RuleTestRequest? request)
    {
        var errors = new List<ValidationErrorDetail>();

        if (request is null)
        {
            errors.Add(new ValidationErrorDetail("request", "Request body is required."));
            return errors;
        }

        var hasRuleId = !string.IsNullOrWhiteSpace(request.RuleId);
        var hasInlineRule = request.Rule is not null;

        if (hasRuleId == hasInlineRule)
        {
            errors.Add(new ValidationErrorDetail("rule", "Provide exactly one of ruleId or rule."));
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            errors.Add(new ValidationErrorDetail("content", "Content is required."));
        }

        if (!string.IsNullOrWhiteSpace(request.Tool) && !AnalysisContractMapper.TryParseTool(request.Tool, out _))
        {
            errors.Add(new ValidationErrorDetail("tool", "Tool must be a supported value."));
        }

        if (!string.IsNullOrWhiteSpace(request.InputType) && !IsSupportedInputType(request.InputType))
        {
            errors.Add(new ValidationErrorDetail("inputType", "InputType must be 'compiler-error' or 'build-log'."));
        }

        if (!string.IsNullOrWhiteSpace(request.ProjectName) && request.ProjectName.Trim().Length > 200)
        {
            errors.Add(new ValidationErrorDetail("projectName", "ProjectName must be 200 characters or fewer."));
        }

        if (!string.IsNullOrWhiteSpace(request.Repository) && request.Repository.Trim().Length > 200)
        {
            errors.Add(new ValidationErrorDetail("repository", "Repository must be 200 characters or fewer."));
        }

        if (request.Rule is not null)
        {
            errors.AddRange(Validate(request.Rule));
        }

        return errors;
    }

    private static IReadOnlyList<ValidationErrorDetail> ValidateCore(
        string? name,
        RuleConditionContract? conditions,
        RuleActionContract? actions)
    {
        var errors = new List<ValidationErrorDetail>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(new ValidationErrorDetail("name", "Name is required."));
        }

        if (conditions is null)
        {
            errors.Add(new ValidationErrorDetail("conditions", "Conditions are required."));
        }
        else
        {
            var hasTrigger =
                !string.IsNullOrWhiteSpace(conditions.Tool) ||
                !string.IsNullOrWhiteSpace(conditions.Code) ||
                !string.IsNullOrWhiteSpace(conditions.Severity) ||
                !string.IsNullOrWhiteSpace(conditions.Category) ||
                !string.IsNullOrWhiteSpace(conditions.MessageRegex) ||
                !string.IsNullOrWhiteSpace(conditions.FilePathRegex) ||
                !string.IsNullOrWhiteSpace(conditions.Fingerprint) ||
                !string.IsNullOrWhiteSpace(conditions.ProjectName) ||
                !string.IsNullOrWhiteSpace(conditions.Repository);

            if (!hasTrigger)
            {
                errors.Add(new ValidationErrorDetail("conditions", "At least one condition trigger is required."));
            }

            if (!string.IsNullOrWhiteSpace(conditions.Tool) &&
                !AnalysisContractMapper.TryParseTool(conditions.Tool, out _))
            {
                errors.Add(new ValidationErrorDetail("conditions.tool", "Tool must be a supported value."));
            }

            if (!string.IsNullOrWhiteSpace(conditions.Severity) &&
                !Enum.TryParse<Severity>(conditions.Severity, ignoreCase: true, out _))
            {
                errors.Add(new ValidationErrorDetail("conditions.severity", "Severity must be a supported value."));
            }

            if (!string.IsNullOrWhiteSpace(conditions.Category) &&
                !Enum.TryParse<DiagnosticCategory>(NormalizeCategory(conditions.Category), ignoreCase: true, out _))
            {
                errors.Add(new ValidationErrorDetail("conditions.category", "Category must be a supported value."));
            }

            if (!string.IsNullOrWhiteSpace(conditions.ProjectName) && conditions.ProjectName.Trim().Length > 200)
            {
                errors.Add(new ValidationErrorDetail("conditions.projectName", "ProjectName must be 200 characters or fewer."));
            }

            if (!string.IsNullOrWhiteSpace(conditions.Repository) && conditions.Repository.Trim().Length > 200)
            {
                errors.Add(new ValidationErrorDetail("conditions.repository", "Repository must be 200 characters or fewer."));
            }

            ValidateRegex(conditions.MessageRegex, "conditions.messageRegex", errors);
            ValidateRegex(conditions.FilePathRegex, "conditions.filePathRegex", errors);
        }

        if (actions is null)
        {
            errors.Add(new ValidationErrorDetail("actions", "Actions are required."));
        }
        else
        {
            var hasAction =
                !string.IsNullOrWhiteSpace(actions.Title) ||
                !string.IsNullOrWhiteSpace(actions.Explanation) ||
                (actions.SuggestedFixes?.Count ?? 0) > 0 ||
                actions.ConfidenceAdjustment.GetValueOrDefault() != 0d ||
                actions.MarkAsPrimaryCause.GetValueOrDefault();

            if (!hasAction)
            {
                errors.Add(new ValidationErrorDetail("actions", "At least one action is required."));
            }
        }

        return errors;
    }

    private static void ValidateRegex(string? value, string field, List<ValidationErrorDetail> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        try
        {
            _ = Regex.IsMatch(string.Empty, value);
        }
        catch (ArgumentException)
        {
            errors.Add(new ValidationErrorDetail(field, "The regex pattern is invalid."));
        }
    }

    private static bool IsSupportedInputType(string value)
        => value.Trim().ToLowerInvariant() is "compiler-error" or "single-diagnostic" or "build-log";

    private static string NormalizeCategory(string value)
    {
        return value
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
    }
}
