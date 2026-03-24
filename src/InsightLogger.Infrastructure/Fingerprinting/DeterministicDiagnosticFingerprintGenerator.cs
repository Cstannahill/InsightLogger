using System;
using System.Security.Cryptography;
using System.Text;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Infrastructure.Fingerprinting;

public sealed class DeterministicDiagnosticFingerprintGenerator
{
    public DiagnosticFingerprint Generate(DiagnosticRecord diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        var normalizedMessage = string.IsNullOrWhiteSpace(diagnostic.NormalizedMessage)
            ? diagnostic.Message
            : diagnostic.NormalizedMessage;

        var canonicalFingerprint = TryGetCanonicalFingerprint(diagnostic, normalizedMessage);
        if (canonicalFingerprint is not null)
        {
            return new DiagnosticFingerprint(canonicalFingerprint);
        }

        var material = string.Join('|',
            diagnostic.ToolKind.ToString(),
            diagnostic.Code?.Trim().ToUpperInvariant() ?? string.Empty,
            diagnostic.Category.ToString(),
            normalizedMessage.Trim().ToLowerInvariant());

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        var hash = Convert.ToHexString(bytes).Substring(0, 16).ToLowerInvariant();
        return new DiagnosticFingerprint($"fp_{hash}");
    }

    private static string? TryGetCanonicalFingerprint(DiagnosticRecord diagnostic, string normalizedMessage)
    {
        var code = diagnostic.Code?.Trim().ToUpperInvariant();

        if (diagnostic.ToolKind == ToolKind.DotNet && code == "CS0103" && diagnostic.Category == DiagnosticCategory.MissingSymbol)
        {
            return "fp_cs0103_name_missing";
        }

        if (diagnostic.ToolKind == ToolKind.DotNet && code == "CS8618" && diagnostic.Category == DiagnosticCategory.NullableSafety)
        {
            return "fp_cs8618_non_nullable_uninitialized";
        }

        if (diagnostic.ToolKind == ToolKind.TypeScript && code == "TS2304" && diagnostic.Category == DiagnosticCategory.MissingSymbol)
        {
            return "fp_ts2304_name_missing";
        }

        if (diagnostic.ToolKind == ToolKind.Python
            && string.Equals(code, "NAMEERROR", StringComparison.OrdinalIgnoreCase)
            && normalizedMessage.Contains("name '{identifier}' is not defined", StringComparison.OrdinalIgnoreCase))
        {
            return "fp_python_nameerror_not_defined";
        }

        return null;
    }
}
