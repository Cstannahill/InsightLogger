using System.Text.RegularExpressions;

namespace InsightLogger.Infrastructure.Parsing.DotNet;

internal static class DotNetDiagnosticNormalizer
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex QuotedIdentifierRegex = new(@"'[^']+'", RegexOptions.Compiled);
    private static readonly Regex QuotedStringRegex = new("\"[^\"]+\"", RegexOptions.Compiled);

    public static string NormalizeWhitespace(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : WhitespaceRegex.Replace(value.Trim(), " ");

    public static string NormalizePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().Replace('\\', '/');
    }

    public static string NormalizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var normalized = NormalizeWhitespace(message);
        normalized = QuotedIdentifierRegex.Replace(normalized, "'{identifier}'");
        normalized = QuotedStringRegex.Replace(normalized, "\"{string}\"");
        return normalized;
    }
}
