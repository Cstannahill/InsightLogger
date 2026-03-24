using System;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Infrastructure.Parsing.Python;

internal static class PythonDiagnosticClassifier
{
    public static DiagnosticCategory Classify(string? code, string normalizedMessage)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            return code switch
            {
                "SyntaxError" or "IndentationError" or "TabError" => DiagnosticCategory.Syntax,
                "NameError" or "UnboundLocalError" or "AttributeError" => DiagnosticCategory.MissingSymbol,
                "ModuleNotFoundError" or "ImportError" => DiagnosticCategory.Dependency,
                "TypeError" or "ValueError" => DiagnosticCategory.TypeMismatch,
                "AssertionError" => DiagnosticCategory.TestFailure,
                "FileNotFoundError" or "PermissionError" or "OSError" or "RuntimeError" or "KeyError" or "IndexError" or "ZeroDivisionError" => DiagnosticCategory.RuntimeEnvironment,
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

        if (normalizedMessage.Contains("not defined", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("has no attribute", StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticCategory.MissingSymbol;
        }

        if (normalizedMessage.Contains("No module named", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("cannot import name", StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticCategory.Dependency;
        }

        if (normalizedMessage.Contains("invalid syntax", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("expected an indented block", StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticCategory.Syntax;
        }

        if (normalizedMessage.Contains("No such file or directory", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("Permission denied", StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticCategory.RuntimeEnvironment;
        }

        return DiagnosticCategory.Unknown;
    }
}
