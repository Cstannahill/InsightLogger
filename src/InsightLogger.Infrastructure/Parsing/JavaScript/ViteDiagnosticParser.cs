using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Fingerprinting;

namespace InsightLogger.Infrastructure.Parsing.JavaScript;

public sealed class ViteDiagnosticParser : IDiagnosticParser
{
    private static readonly Regex ResolveImportPattern = new(
        """(?:\[vite\]\s*:\s*)?Rollup\s+failed\s+to\s+resolve\s+import\s+"(?<import>[^"]+)"\s+from\s+"(?<importer>[^"]+)"\.?""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex MissingExportPattern = new(
        """(?<file>[^\r\n]+?)\s*\((?<line>\d+):(?<column>\d+)\):\s*"(?<symbol>[^"]+)"\s+is\s+not\s+exported\s+by\s+"(?<module>[^"]+)"(?:,\s*imported\s+by\s+"(?<importer>[^"]+)")?\.""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private static readonly Regex ParseErrorPattern = new(
        """(?<file>[^\r\n]+?)\s*\((?<line>\d+):(?<column>\d+)\):\s*(?<message>(?:Unexpected|Expected|Unterminated).+)$""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private static readonly Regex FileLocationPattern = new(
        """^file:\s*(?<file>.+?)(?::(?<line>\d+):(?<column>\d+))?\s*$""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private readonly DeterministicDiagnosticFingerprintGenerator _fingerprintGenerator = new();

    public string Name => "vite-diagnostic-parser-v1";

    public ToolKind ToolKind => ToolKind.Vite;

    public bool CanHandle(ParseDiagnosticsRequest request)
    {
        if (request.ToolHint == ToolKind.Vite)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return false;
        }

        return request.Content.Contains("[vite]", StringComparison.OrdinalIgnoreCase)
               || request.Content.Contains("vite v", StringComparison.OrdinalIgnoreCase)
               || request.Content.Contains("error during build:", StringComparison.OrdinalIgnoreCase)
               || request.Content.Contains("Rollup failed to resolve import", StringComparison.OrdinalIgnoreCase)
               || request.Content.Contains("is not exported by", StringComparison.OrdinalIgnoreCase);
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
            ToolKind: ToolKind.Vite,
            ParserName: Name,
            ParseConfidence: diagnostics.Count == 1 ? 1d : 0d,
            Diagnostics: diagnostics,
            TotalSegments: 1,
            ParsedSegments: diagnostics.Count,
            UnparsedSegments: unparsedSegments));
    }

    private bool TryParseSegment(string segment, out DiagnosticRecord? diagnostic)
    {
        var match = ResolveImportPattern.Match(segment);
        if (match.Success)
        {
            var importTarget = match.Groups["import"].Value.Trim();
            var importerPath = JavaScriptDiagnosticNormalizer.NormalizePath(match.Groups["importer"].Value);
            var message = $"Rollup failed to resolve import \"{importTarget}\" from \"{importerPath}\".";
            var normalizedMessage = JavaScriptDiagnosticNormalizer.NormalizeMessage("VITE_RESOLVE_IMPORT", message);
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["import"] = importTarget,
                ["importer"] = importerPath
            };

            diagnostic = BuildDiagnostic(
                code: "VITE_RESOLVE_IMPORT",
                message: message,
                normalizedMessage: normalizedMessage,
                category: JavaScriptDiagnosticClassifier.Classify("VITE_RESOLVE_IMPORT", normalizedMessage),
                location: new DiagnosticLocation(importerPath, null, null),
                metadata: metadata,
                rawSnippet: segment);

            return true;
        }

        match = MissingExportPattern.Match(segment);
        if (match.Success)
        {
            var filePath = JavaScriptDiagnosticNormalizer.NormalizePath(match.Groups["file"].Value);
            var module = JavaScriptDiagnosticNormalizer.NormalizePath(match.Groups["module"].Value);
            var symbol = match.Groups["symbol"].Value.Trim();
            var message = $"\"{symbol}\" is not exported by \"{module}\".";
            var normalizedMessage = JavaScriptDiagnosticNormalizer.NormalizeMessage("VITE_MISSING_EXPORT", message);
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["symbol"] = symbol,
                ["module"] = module
            };

            if (match.Groups["importer"].Success)
            {
                metadata["importer"] = JavaScriptDiagnosticNormalizer.NormalizePath(match.Groups["importer"].Value);
            }

            diagnostic = BuildDiagnostic(
                code: "VITE_MISSING_EXPORT",
                message: message,
                normalizedMessage: normalizedMessage,
                category: JavaScriptDiagnosticClassifier.Classify("VITE_MISSING_EXPORT", normalizedMessage),
                location: new DiagnosticLocation(
                    filePath,
                    ParseNullableInt(match.Groups["line"].Value),
                    ParseNullableInt(match.Groups["column"].Value)),
                metadata: metadata,
                rawSnippet: segment);

            return true;
        }

        match = ParseErrorPattern.Match(segment);
        if (match.Success)
        {
            var filePath = JavaScriptDiagnosticNormalizer.NormalizePath(match.Groups["file"].Value);
            var message = JavaScriptDiagnosticNormalizer.NormalizeWhitespace(match.Groups["message"].Value);
            var normalizedMessage = JavaScriptDiagnosticNormalizer.NormalizeMessage("VITE_PARSE_ERROR", $"{message} in \"{filePath}\"");

            diagnostic = BuildDiagnostic(
                code: "VITE_PARSE_ERROR",
                message: message,
                normalizedMessage: normalizedMessage,
                category: JavaScriptDiagnosticClassifier.Classify("VITE_PARSE_ERROR", normalizedMessage),
                location: new DiagnosticLocation(
                    filePath,
                    ParseNullableInt(match.Groups["line"].Value),
                    ParseNullableInt(match.Groups["column"].Value)),
                metadata: null,
                rawSnippet: segment);

            return true;
        }

        var fallbackMessage = ResolveFallbackMessage(segment);
        if (!string.IsNullOrWhiteSpace(fallbackMessage))
        {
            var normalizedMessage = JavaScriptDiagnosticNormalizer.NormalizeMessage("VITE_BUILD_ERROR", fallbackMessage);
            var locationMatch = FileLocationPattern.Match(segment);
            var location = locationMatch.Success
                ? new DiagnosticLocation(
                    JavaScriptDiagnosticNormalizer.NormalizePath(locationMatch.Groups["file"].Value),
                    ParseNullableInt(locationMatch.Groups["line"].Value),
                    ParseNullableInt(locationMatch.Groups["column"].Value))
                : null;

            diagnostic = BuildDiagnostic(
                code: "VITE_BUILD_ERROR",
                message: fallbackMessage,
                normalizedMessage: normalizedMessage,
                category: JavaScriptDiagnosticClassifier.Classify("VITE_BUILD_ERROR", normalizedMessage),
                location: location,
                metadata: null,
                rawSnippet: segment);

            return true;
        }

        diagnostic = null;
        return false;
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
            toolKind: ToolKind.Vite,
            severity: Severity.Error,
            message: message,
            rawSnippet: rawSnippet,
            source: "vite",
            code: code,
            normalizedMessage: normalizedMessage,
            location: location,
            category: category,
            metadata: metadata);

        return record.WithFingerprint(_fingerprintGenerator.Generate(record));
    }

    private static string? ResolveFallbackMessage(string segment)
    {
        foreach (var rawLine in segment.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)
                || line.StartsWith("vite v", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("transforming", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("✓", StringComparison.Ordinal)
                || line.StartsWith("x Build failed", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("error during build:", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("at ", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return JavaScriptDiagnosticNormalizer.NormalizeWhitespace(line);
        }

        return null;
    }

    private static int? ParseNullableInt(string value) =>
        int.TryParse(value, out var parsed) ? parsed : null;
}
