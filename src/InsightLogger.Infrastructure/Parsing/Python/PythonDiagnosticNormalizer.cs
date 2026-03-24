using System.Text.RegularExpressions;

namespace InsightLogger.Infrastructure.Parsing.Python;

internal static partial class PythonDiagnosticNormalizer
{
    public static string NormalizeWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return MultiWhitespacePattern().Replace(value.Trim(), " ");
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
            "NameError" => NameErrorPattern().Replace(normalized, "name '{identifier}' is not defined"),
            "UnboundLocalError" => UnboundLocalPattern().Replace(normalized, "local variable '{identifier}' referenced before assignment"),
            "ModuleNotFoundError" => ModuleNotFoundPattern().Replace(normalized, "No module named '{module}'"),
            "ImportError" => ImportErrorPattern().Replace(normalized, "cannot import name '{identifier}' from '{module}'"),
            "AttributeError" => AttributeErrorPattern().Replace(normalized, "'{type}' object has no attribute '{attribute}'"),
            "TypeError" => TypeObjectPattern().Replace(normalized, "'{type}' object is {operation}"),
            "FileNotFoundError" => FileNotFoundPattern().Replace(normalized, "[Errno {number}] No such file or directory: '{path}'"),
            _ => normalized
        };

        normalized = SingleQuotedPathPattern().Replace(normalized, "'{path}'");
        normalized = IntegerPattern().Replace(normalized, "{number}");
        normalized = SingleQuotedStringPattern().Replace(normalized, "'{identifier}'");

        return normalized;
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MultiWhitespacePattern();

    [GeneratedRegex(@"'(?:(?:[A-Za-z]:)?[/\\][^'\r\n]+|\.?\.?[/\\][^'\r\n]+)'", RegexOptions.Compiled)]
    private static partial Regex SingleQuotedPathPattern();

    [GeneratedRegex(@"'(?!\{(?:identifier|module|path|type|attribute)\})[^'\r\n]+'", RegexOptions.Compiled)]
    private static partial Regex SingleQuotedStringPattern();

    [GeneratedRegex(@"\b\d+\b", RegexOptions.Compiled)]
    private static partial Regex IntegerPattern();

    [GeneratedRegex(@"^name\s+'[^']+'\s+is\s+not\s+defined$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex NameErrorPattern();

    [GeneratedRegex(@"^local\s+variable\s+'[^']+'\s+referenced\s+before\s+assignment$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex UnboundLocalPattern();

    [GeneratedRegex(@"^No\s+module\s+named\s+'[^']+'$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ModuleNotFoundPattern();

    [GeneratedRegex(@"^cannot\s+import\s+name\s+'[^']+'\s+from\s+'[^']+'.*$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ImportErrorPattern();

    [GeneratedRegex(@"^'[^']+'\s+object\s+has\s+no\s+attribute\s+'[^']+'$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex AttributeErrorPattern();

    [GeneratedRegex(@"^'[^']+'\s+object\s+is\s+.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex TypeObjectPattern();

    [GeneratedRegex(@"^\[Errno\s+\d+\]\s+No\s+such\s+file\s+or\s+directory:\s+'[^']+'$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex FileNotFoundPattern();
}
