using System.Text.RegularExpressions;

namespace InsightLogger.Infrastructure.Parsing.JavaScript;

internal static class JavaScriptDiagnosticNormalizer
{
    private static readonly Regex MultiWhitespacePattern = new("\\s+", RegexOptions.Compiled);
    private static readonly Regex ViteResolveImportPattern = new("Rollup\\s+failed\\s+to\\s+resolve\\s+import\\s+\"[^\"]+\"\\s+from\\s+\"[^\"]+\"\\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ViteMissingExportPattern = new("\"[^\"]+\"\\s+is\\s+not\\s+exported\\s+by\\s+\"[^\"]+\".*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ViteUnexpectedTokenPattern = new("Unexpected\\s+token.*\\sin\\s+\"[^\"]+\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MissingScriptPattern = new("Missing\\s+script:\\s+\"[^\"]+\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EresolvePattern = new("ERESOLVE\\s+unable\\s+to\\s+resolve\\s+dependency\\s+tree", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PackageJsonMissingPattern = new("Could\\s+not\\s+read\\s+package\\.json.*?(?:'|\\\")(?:(?:[A-Za-z]:)?[/\\\\][^'\\\"\\r\\n]*package\\.json)(?:'|\\\")", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex QuotedPathPattern = new("'(?:(?:[A-Za-z]:)?[/\\\\][^'\\r\\n]+|\\.\\.?[/\\\\][^'\\r\\n]+)'|\\\"(?:(?:[A-Za-z]:)?[/\\\\][^\\\"\\r\\n]+|\\.\\.?[/\\\\][^\\\"\\r\\n]+)\\\"", RegexOptions.Compiled);

    public static string NormalizeWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return MultiWhitespacePattern.Replace(value.Trim(), " ");
    }

    public static string NormalizePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().Replace('\\', '/');
    }

    public static string NormalizeMessage(string code, string? message)
    {
        var normalized = NormalizeWhitespace(message);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return code.Trim();
        }

        normalized = code switch
        {
            "VITE_RESOLVE_IMPORT" => ViteResolveImportPattern.Replace(normalized, "Rollup failed to resolve import '{import}' from '{file}'."),
            "VITE_MISSING_EXPORT" => ViteMissingExportPattern.Replace(normalized, "Export '{symbol}' is not provided by '{module}'."),
            "VITE_PARSE_ERROR" => ViteUnexpectedTokenPattern.Replace(normalized, "Unexpected token in '{file}'."),
            "NPM_MISSING_SCRIPT" => MissingScriptPattern.Replace(normalized, "Missing script '{script}'."),
            "ERESOLVE" => EresolvePattern.Replace(normalized, "unable to resolve dependency tree"),
            "ENOENT" => PackageJsonMissingPattern.Replace(normalized, "Could not read package.json at '{path}'."),
            _ => normalized
        };

        normalized = QuotedPathPattern.Replace(normalized, "'{path}'");
        return normalized;
    }
}
