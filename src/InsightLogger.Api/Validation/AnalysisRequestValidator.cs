using System;
using System.Collections.Generic;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Api.Mapping;
using InsightLogger.Contracts.Common;

namespace InsightLogger.Api.Validation;

public static class AnalysisRequestValidator
{
    private const int MaxContentLength = 250_000;

    public static IReadOnlyList<ValidationErrorDetail> Validate(AnalyzeBuildLogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = ValidateCommon(request.Tool, request.Content);
        return errors;
    }

    public static IReadOnlyList<ValidationErrorDetail> Validate(AnalyzeCompilerErrorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = ValidateCommon(request.Tool, request.Content);
        return errors;
    }

    private static List<ValidationErrorDetail> ValidateCommon(string? tool, string? content)
    {
        var errors = new List<ValidationErrorDetail>();

        if (string.IsNullOrWhiteSpace(content))
        {
            errors.Add(new ValidationErrorDetail("content", "Content is required."));
        }
        else if (content.Length > MaxContentLength)
        {
            errors.Add(new ValidationErrorDetail("content", $"Content must be {MaxContentLength} characters or fewer."));
        }

        if (!string.IsNullOrWhiteSpace(tool) && !AnalysisContractMapper.TryParseTool(tool, out _))
        {
            errors.Add(new ValidationErrorDetail("tool", "Tool must be one of: dotnet, typescript, npm, vite, python, generic."));
        }

        return errors;
    }
}
