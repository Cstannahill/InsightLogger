using System.Text.RegularExpressions;

namespace InsightLogger.Application.Analyses.Persistence;

internal static partial class StoredRawContentPrivacyFilter
{
    public static (string Content, bool WasRedacted) Apply(string content)
    {
        var redacted = content;
        redacted = UrlRegex().Replace(redacted, "[redacted-url]");
        redacted = WindowsPathRegex().Replace(redacted, "[redacted-path]");
        redacted = UnixPathRegex().Replace(redacted, "[redacted-path]");
        redacted = EmailRegex().Replace(redacted, "[redacted-email]");
        redacted = BearerTokenRegex().Replace(redacted, "Bearer [redacted-token]");
        redacted = SecretAssignmentRegex().Replace(redacted, static match =>
        {
            var key = match.Groups[1].Value;
            var replacement = key.Contains("token", System.StringComparison.OrdinalIgnoreCase)
                ? "[redacted-token]"
                : "[redacted-secret]";

            return $"{key}={replacement}";
        });

        return (redacted, !string.Equals(content, redacted, System.StringComparison.Ordinal));
    }

    [GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"\b[A-Za-z]:\\(?:[^\s:;""'<>|]+\\)*[^\s:;""'<>|]*", RegexOptions.Compiled)]
    private static partial Regex WindowsPathRegex();

    [GeneratedRegex(@"(?<![A-Za-z0-9_\-])/(?:[^\s/]+/)*[^\s/:]+", RegexOptions.Compiled)]
    private static partial Regex UnixPathRegex();

    [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9._\-+/=]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex(@"(?i)\b(token|secret|password|api[_-]?key)\b\s*[:=]\s*([^\s,;]+)", RegexOptions.Compiled)]
    private static partial Regex SecretAssignmentRegex();
}
