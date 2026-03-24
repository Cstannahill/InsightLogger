using System;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Infrastructure.Parsing.DotNet;

internal static class DotNetDiagnosticClassifier
{
    public static DiagnosticCategory Classify(string? code, string normalizedMessage)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            if (code.StartsWith("MSB", StringComparison.OrdinalIgnoreCase)
                || code.StartsWith("NETSDK", StringComparison.OrdinalIgnoreCase))
            {
                return DiagnosticCategory.BuildSystem;
            }

            if (code.StartsWith("NU", StringComparison.OrdinalIgnoreCase)
                || code.Equals("CS0246", StringComparison.OrdinalIgnoreCase)
                || code.Equals("CS0234", StringComparison.OrdinalIgnoreCase))
            {
                return DiagnosticCategory.Dependency;
            }

            if (code.Equals("CS0103", StringComparison.OrdinalIgnoreCase))
            {
                return DiagnosticCategory.MissingSymbol;
            }

            if (code.Equals("CS8618", StringComparison.OrdinalIgnoreCase))
            {
                return DiagnosticCategory.NullableSafety;
            }
        }

        return normalizedMessage.Contains("does not exist in the current context", StringComparison.OrdinalIgnoreCase)
            ? DiagnosticCategory.MissingSymbol
            : DiagnosticCategory.Unknown;
    }
}
