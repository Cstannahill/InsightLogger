using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Fingerprinting;

namespace InsightLogger.Infrastructure.Parsing.TypeScript;

public sealed partial class TypeScriptDiagnosticParser : IDiagnosticParser
{
    private static readonly Regex StartLinePattern = CreateStartLinePattern();
    private readonly DeterministicDiagnosticFingerprintGenerator _fingerprintGenerator = new();

    public string Name => "typescript-diagnostic-parser-v1";

    public ToolKind ToolKind => ToolKind.TypeScript;

    public bool CanHandle(ParseDiagnosticsRequest request)
    {
        if (request.ToolHint == ToolKind.TypeScript)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return false;
        }

        return StartLinePattern.IsMatch(request.Content)
               || request.Content.Contains(" error TS", StringComparison.OrdinalIgnoreCase)
               || request.Content.Contains(" warning TS", StringComparison.OrdinalIgnoreCase)
               || Regex.IsMatch(request.Content, @"\bTS\d{4,5}\b", RegexOptions.IgnoreCase);
    }

    public Task<ParseDiagnosticsResult> ParseAsync(ParseDiagnosticsRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Content);

        var segments = Segment(request.Content);
        var diagnostics = new List<DiagnosticRecord>(segments.Count);
        var unparsedSegments = request.CaptureUnparsedSegments ? new List<string>() : null;

        foreach (var segment in segments)
        {
            if (TryParseSegment(segment, out var diagnostic))
            {
                diagnostics.Add(diagnostic!);
                continue;
            }

            unparsedSegments?.Add(segment);
        }

        var parseConfidence = segments.Count == 0
            ? 0d
            : Math.Round((double)diagnostics.Count / segments.Count, 4, MidpointRounding.AwayFromZero);

        return Task.FromResult(new ParseDiagnosticsResult(
            ToolKind: ToolKind.TypeScript,
            ParserName: Name,
            ParseConfidence: parseConfidence,
            Diagnostics: diagnostics,
            TotalSegments: segments.Count,
            ParsedSegments: diagnostics.Count,
            UnparsedSegments: unparsedSegments));
    }

    private static IReadOnlyList<string> Segment(string content)
    {
        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var segments = new List<string>();
        var current = new StringBuilder();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                if (current.Length > 0)
                {
                    current.AppendLine();
                }

                continue;
            }

            if (LooksLikeDiagnosticStart(line))
            {
                FlushCurrentSegment();
                current.Append(line);
                continue;
            }

            if (current.Length > 0 && IsContinuationLine(line))
            {
                current.AppendLine();
                current.Append(line);
                continue;
            }

            FlushCurrentSegment();
        }

        FlushCurrentSegment();
        return segments;

        void FlushCurrentSegment()
        {
            if (current.Length == 0)
            {
                return;
            }

            segments.Add(current.ToString());
            current.Clear();
        }
    }

    private static bool LooksLikeDiagnosticStart(string line) => StartLinePattern.IsMatch(line);

    private static bool IsContinuationLine(string line)
    {
        if (LooksLikeDiagnosticStart(line))
        {
            return false;
        }

        if (line.StartsWith("Found ", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("error Command failed", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("npm ERR!", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return char.IsWhiteSpace(line[0])
               || char.IsDigit(line[0])
               || line.StartsWith("~", StringComparison.Ordinal)
               || line.StartsWith("Did you mean", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryParseSegment(string segment, out DiagnosticRecord? diagnostic)
    {
        var firstLineBreak = segment.IndexOf('\n');
        var header = firstLineBreak >= 0 ? segment[..firstLineBreak] : segment;
        var match = StartLinePattern.Match(header);
        if (!match.Success)
        {
            diagnostic = null;
            return false;
        }

        var filePath = TypeScriptDiagnosticNormalizer.NormalizePath(match.Groups["file"].Value);
        var message = TypeScriptDiagnosticNormalizer.NormalizeWhitespace(match.Groups["message"].Value);
        var normalizedMessage = TypeScriptDiagnosticNormalizer.NormalizeMessage(message);
        var code = match.Groups["code"].Value.Trim().ToUpperInvariant();
        var severity = ParseSeverity(match.Groups["severity"].Value);
        var category = TypeScriptDiagnosticClassifier.Classify(code, normalizedMessage);
        var location = new DiagnosticLocation(
            FilePath: string.IsNullOrWhiteSpace(filePath) ? null : filePath,
            Line: ParseNullableInt(match.Groups["line"].Value),
            Column: ParseNullableInt(match.Groups["column"].Value));

        var diagnosticRecord = new DiagnosticRecord(
            toolKind: ToolKind.TypeScript,
            severity: severity,
            message: message,
            rawSnippet: segment,
            source: "tsc",
            code: code,
            normalizedMessage: normalizedMessage,
            location: location,
            category: category);

        diagnostic = diagnosticRecord.WithFingerprint(_fingerprintGenerator.Generate(diagnosticRecord));
        return true;
    }

    private static int? ParseNullableInt(string value) =>
        int.TryParse(value, out var parsed) ? parsed : null;

    private static Severity ParseSeverity(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "warning" => Severity.Warning,
            "error" => Severity.Error,
            _ => Severity.Unknown
        };

    [GeneratedRegex(@"^(?<file>.+?)(?::|\()(?<line>\d+)(?:,|:)(?<column>\d+)\)?\s*(?:-|:)\s*(?<severity>error|warning)\s+(?<code>TS\d{4,5})\s*:\s*(?<message>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex CreateStartLinePattern();
}
