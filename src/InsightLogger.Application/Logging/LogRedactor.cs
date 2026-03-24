using System.Text.RegularExpressions;

namespace InsightLogger.Application.Logging;

public static partial class LogRedactor
{
    private const int DefaultMaxLength = 240;

    public static string? Redact(string? value, int maxLength = DefaultMaxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var redacted = value.Trim();
        redacted = SecretAssignmentPattern().Replace(redacted, "$1=<redacted>");
        redacted = BearerTokenPattern().Replace(redacted, "Bearer <redacted>");
        redacted = UrlPattern().Replace(redacted, "<redacted:url>");
        redacted = WindowsPathPattern().Replace(redacted, "<redacted:path>");
        redacted = UnixPathPattern().Replace(redacted, "<redacted:path>");
        redacted = EmailPattern().Replace(redacted, "<redacted:email>");
        redacted = CollapseWhitespacePattern().Replace(redacted, " ");

        if (redacted.Length > maxLength)
        {
            redacted = redacted[..maxLength] + "…";
        }

        return redacted;
    }

    [GeneratedRegex(@"(?i)\b(api[_-]?key|access[_-]?token|refresh[_-]?token|token|secret|password)\b\s*[:=]\s*([^\s,;]+)")]
    private static partial Regex SecretAssignmentPattern();

    [GeneratedRegex(@"(?i)\bBearer\s+[A-Za-z0-9\-._~+/]+=*")]
    private static partial Regex BearerTokenPattern();

    [GeneratedRegex("https?://[^\\s\"']+")]
    private static partial Regex UrlPattern();

    [GeneratedRegex("(?:[a-zA-Z]:\\\\|\\\\\\\\)[^\\s\"']+")]
    private static partial Regex WindowsPathPattern();

    [GeneratedRegex("(?<!\\w)/(?:[^/\\s\"']+/)+[^/\\s\"']+")]
    private static partial Regex UnixPathPattern();

    [GeneratedRegex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex CollapseWhitespacePattern();
}
