using System;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Infrastructure.Parsing.TypeScript;

internal static class TypeScriptDiagnosticClassifier
{
    public static DiagnosticCategory Classify(string? code, string normalizedMessage)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return ClassifyByMessage(normalizedMessage);
        }

        return code.Trim().ToUpperInvariant() switch
        {
            "TS2304" or "TS2552" or "TS2503" or "TS2339" => DiagnosticCategory.MissingSymbol,
            "TS2307" => DiagnosticCategory.Dependency,
            "TS2322" or "TS2345" or "TS2741" => DiagnosticCategory.TypeMismatch,
            "TS1005" or "TS1109" or "TS1128" => DiagnosticCategory.Syntax,
            _ => ClassifyByMessage(normalizedMessage)
        };
    }

    private static DiagnosticCategory ClassifyByMessage(string normalizedMessage)
    {
        if (normalizedMessage.Contains("Cannot find name", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("Cannot find namespace", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("does not exist on type", StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticCategory.MissingSymbol;
        }

        if (normalizedMessage.Contains("Cannot find module", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("type declarations", StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticCategory.Dependency;
        }

        if (normalizedMessage.Contains("is not assignable to type", StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticCategory.TypeMismatch;
        }

        return DiagnosticCategory.Unknown;
    }
}
