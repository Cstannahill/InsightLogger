using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Fingerprinting;

namespace InsightLogger.Infrastructure.Parsing.JavaScript;

public sealed class NpmDiagnosticParser : IDiagnosticParser
{
    private static readonly Regex ErrorPrefixPattern = new("^npm\\s+(?:ERR!|error)\\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MissingScriptPattern = new("^npm\\s+(?:ERR!|error)\\s+Missing\\s+script:\\s+\"?(?<script>[^\"\\r\\n]+)\"?\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex CodePattern = new("^npm\\s+(?:ERR!|error)\\s+code\\s+(?<code>[A-Z0-9_]+)\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex PathPattern = new("^npm\\s+(?:ERR!|error)\\s+path\\s+(?<path>.+?)\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex PackageJsonMessagePattern = new("Could\\s+not\\s+read\\s+package\\.json:\\s+Error:\\s+ENOENT:.*?(?:'|\\\")(?<path>[^'\\\"\\r\\n]*package\\.json)(?:'|\\\")", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly DeterministicDiagnosticFingerprintGenerator _fingerprintGenerator = new();

    public string Name => "npm-diagnostic-parser-v1";

    public ToolKind ToolKind => ToolKind.Npm;

    public bool CanHandle(ParseDiagnosticsRequest request)
    {
        if (request.ToolHint == ToolKind.Npm)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return false;
        }

        return request.Content.Contains("npm ERR!", StringComparison.OrdinalIgnoreCase)
               || request.Content.Contains("npm error", StringComparison.OrdinalIgnoreCase)
               || request.Content.Contains("Missing script:", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ParseDiagnosticsResult> ParseAsync(ParseDiagnosticsRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Content);

        var segment = request.Content.Trim();
        var diagnostics = new List<DiagnosticRecord>(1);
        var unparsedSegments = request.CaptureUnparsedSegments ? new List<string>() : null;

        if (TryParseSegment(segment, out var diagnostic))
        {
            diagnostics.Add(diagnostic!);
        }
        else
        {
            unparsedSegments?.Add(segment);
        }

        return Task.FromResult(new ParseDiagnosticsResult(
            ToolKind: ToolKind.Npm,
            ParserName: Name,
            ParseConfidence: diagnostics.Count == 1 ? 1d : 0d,
            Diagnostics: diagnostics,
            TotalSegments: 1,
            ParsedSegments: diagnostics.Count,
            UnparsedSegments: unparsedSegments));
    }

    private bool TryParseSegment(string segment, out DiagnosticRecord? diagnostic)
    {
        var match = MissingScriptPattern.Match(segment);
        if (match.Success)
        {
            var scriptName = match.Groups["script"].Value.Trim();
            var message = $"Missing script: \"{scriptName}\"";
            var scriptNormalizedMessage = JavaScriptDiagnosticNormalizer.NormalizeMessage("NPM_MISSING_SCRIPT", message);
            var scriptMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["script"] = scriptName
            };

            diagnostic = BuildDiagnostic(
                code: "NPM_MISSING_SCRIPT",
                message: message,
                normalizedMessage: scriptNormalizedMessage,
                category: JavaScriptDiagnosticClassifier.Classify("NPM_MISSING_SCRIPT", scriptNormalizedMessage),
                location: null,
                metadata: scriptMetadata,
                rawSnippet: segment);

            return true;
        }

        var code = ResolveCode(segment);
        var messageText = ResolvePrimaryMessage(segment);
        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(messageText))
        {
            diagnostic = null;
            return false;
        }

        code ??= "NPM_BUILD_ERROR";
        messageText ??= "npm command failed";

        DiagnosticLocation? location = null;
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        match = PathPattern.Match(segment);
        if (match.Success)
        {
            var path = JavaScriptDiagnosticNormalizer.NormalizePath(match.Groups["path"].Value);
            location = new DiagnosticLocation(path, null, null);
            metadata["path"] = path;
        }
        else
        {
            match = PackageJsonMessagePattern.Match(segment);
            if (match.Success)
            {
                var path = JavaScriptDiagnosticNormalizer.NormalizePath(match.Groups["path"].Value);
                location = new DiagnosticLocation(path, null, null);
                metadata["path"] = path;
            }
        }

        var normalizedMessage = JavaScriptDiagnosticNormalizer.NormalizeMessage(code, messageText);

        diagnostic = BuildDiagnostic(
            code: code,
            message: messageText,
            normalizedMessage: normalizedMessage,
            category: JavaScriptDiagnosticClassifier.Classify(code, normalizedMessage),
            location: location,
            metadata: metadata.Count == 0 ? null : metadata,
            rawSnippet: segment);

        return true;
    }

    private DiagnosticRecord BuildDiagnostic(
        string code,
        string message,
        string normalizedMessage,
        DiagnosticCategory category,
        DiagnosticLocation? location,
        IReadOnlyDictionary<string, string>? metadata,
        string rawSnippet)
    {
        var record = new DiagnosticRecord(
            toolKind: ToolKind.Npm,
            severity: Severity.Error,
            message: message,
            rawSnippet: rawSnippet,
            source: "npm",
            code: code,
            normalizedMessage: normalizedMessage,
            location: location,
            category: category,
            metadata: metadata);

        return record.WithFingerprint(_fingerprintGenerator.Generate(record));
    }

    private static string? ResolveCode(string segment)
    {
        var codeMatch = CodePattern.Match(segment);
        if (codeMatch.Success)
        {
            return codeMatch.Groups["code"].Value.Trim().ToUpperInvariant();
        }

        foreach (var rawLine in segment.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = rawLine.Trim();
            if (!ErrorPrefixPattern.IsMatch(line))
            {
                continue;
            }

            var stripped = ErrorPrefixPattern.Replace(line, string.Empty).Trim();
            if (stripped.StartsWith("ERESOLVE ", StringComparison.OrdinalIgnoreCase))
            {
                return "ERESOLVE";
            }

            if (stripped.StartsWith("ENOENT", StringComparison.OrdinalIgnoreCase))
            {
                return "ENOENT";
            }
        }

        return null;
    }

    private static string? ResolvePrimaryMessage(string segment)
    {
        foreach (var rawLine in segment.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = rawLine.Trim();
            if (!ErrorPrefixPattern.IsMatch(line))
            {
                continue;
            }

            var stripped = ErrorPrefixPattern.Replace(line, string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(stripped)
                || stripped.StartsWith("code ", StringComparison.OrdinalIgnoreCase)
                || stripped.StartsWith("path ", StringComparison.OrdinalIgnoreCase)
                || stripped.StartsWith("errno ", StringComparison.OrdinalIgnoreCase)
                || stripped.StartsWith("cwd ", StringComparison.OrdinalIgnoreCase)
                || stripped.StartsWith("syscall ", StringComparison.OrdinalIgnoreCase)
                || stripped.StartsWith("A complete log", StringComparison.OrdinalIgnoreCase)
                || stripped.StartsWith("This is related to npm", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return stripped;
        }

        return null;
    }
}
