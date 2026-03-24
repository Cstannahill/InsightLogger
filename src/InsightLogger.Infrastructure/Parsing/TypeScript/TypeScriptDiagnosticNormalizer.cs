using System;
using System.Text.RegularExpressions;

namespace InsightLogger.Infrastructure.Parsing.TypeScript;

internal static partial class TypeScriptDiagnosticNormalizer
{
    private static readonly Regex WhitespaceRegex = CreateWhitespaceRegex();
    private static readonly Regex CannotFindNameRegex = CreateCannotFindNameRegex();
    private static readonly Regex CannotFindNamespaceRegex = CreateCannotFindNamespaceRegex();
    private static readonly Regex CannotFindModuleRegex = CreateCannotFindModuleRegex();
    private static readonly Regex PropertyDoesNotExistRegex = CreatePropertyDoesNotExistRegex();
    private static readonly Regex TypeNotAssignableRegex = CreateTypeNotAssignableRegex();
    private static readonly Regex ArgumentNotAssignableRegex = CreateArgumentNotAssignableRegex();

    public static string NormalizePath(string path) =>
        string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Trim().Replace('\\', '/');

    public static string NormalizeWhitespace(string message) =>
        string.IsNullOrWhiteSpace(message)
            ? string.Empty
            : WhitespaceRegex.Replace(message.Trim(), " ");

    public static string NormalizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var normalized = NormalizeWhitespace(message);

        normalized = CannotFindNameRegex.Replace(normalized, "Cannot find name '{identifier}'.");
        normalized = CannotFindNamespaceRegex.Replace(normalized, "Cannot find namespace '{identifier}'.");
        normalized = CannotFindModuleRegex.Replace(normalized, "Cannot find module '{module}'$1");
        normalized = PropertyDoesNotExistRegex.Replace(normalized, "Property '{identifier}' does not exist on type '{type}'.");
        normalized = TypeNotAssignableRegex.Replace(normalized, "Type '{type}' is not assignable to type '{type}'.");
        normalized = ArgumentNotAssignableRegex.Replace(normalized, "Argument of type '{type}' is not assignable to parameter of type '{type}'.");

        return normalized;
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex CreateWhitespaceRegex();

    [GeneratedRegex(@"^Cannot find name '[^']+'\.$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex CreateCannotFindNameRegex();

    [GeneratedRegex(@"^Cannot find namespace '[^']+'\.$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex CreateCannotFindNamespaceRegex();

    [GeneratedRegex(@"^Cannot find module '[^']+'((?: or its corresponding type declarations)?\.)$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex CreateCannotFindModuleRegex();

    [GeneratedRegex(@"^Property '[^']+' does not exist on type '[^']+'\.$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex CreatePropertyDoesNotExistRegex();

    [GeneratedRegex(@"^Type '.+?' is not assignable to type '.+?'\.$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex CreateTypeNotAssignableRegex();

    [GeneratedRegex(@"^Argument of type '.+?' is not assignable to parameter of type '.+?'\.$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex CreateArgumentNotAssignableRegex();
}
