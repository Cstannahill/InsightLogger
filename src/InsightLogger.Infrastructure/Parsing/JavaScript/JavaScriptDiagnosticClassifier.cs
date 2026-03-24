using System;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Infrastructure.Parsing.JavaScript;

internal static class JavaScriptDiagnosticClassifier
{
    public static DiagnosticCategory Classify(string? code, string normalizedMessage)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            return code.Trim().ToUpperInvariant() switch
            {
                "VITE_RESOLVE_IMPORT" => DiagnosticCategory.Dependency,
                "VITE_MISSING_EXPORT" => DiagnosticCategory.MissingSymbol,
                "VITE_PARSE_ERROR" => DiagnosticCategory.Syntax,
                "NPM_MISSING_SCRIPT" => DiagnosticCategory.Configuration,
                "ERESOLVE" => DiagnosticCategory.Dependency,
                "ENOENT" => DiagnosticCategory.Configuration,
                "ELIFECYCLE" => DiagnosticCategory.BuildSystem,
                "ERR_MODULE_NOT_FOUND" => DiagnosticCategory.Dependency,
                "VITE_BUILD_ERROR" => ClassifyByMessage(normalizedMessage),
                _ => ClassifyByMessage(normalizedMessage)
            };
        }

        return ClassifyByMessage(normalizedMessage);
    }

    private static DiagnosticCategory ClassifyByMessage(string normalizedMessage)
    {
        if (string.IsNullOrWhiteSpace(normalizedMessage))
        {
            return DiagnosticCategory.Unknown;
        }

        if (normalizedMessage.Contains("failed to resolve import", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("dependency tree", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("module not found", StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticCategory.Dependency;
        }

        if (normalizedMessage.Contains("not exported by", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("not provided by", StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticCategory.MissingSymbol;
        }

        if (normalizedMessage.Contains("Unexpected token", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("Expected", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("Unterminated", StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticCategory.Syntax;
        }

        if (normalizedMessage.Contains("Missing script", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("package.json", StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticCategory.Configuration;
        }

        if (normalizedMessage.Contains("command failed", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("lifecycle", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("error during build", StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticCategory.BuildSystem;
        }

        return DiagnosticCategory.Unknown;
    }
}
