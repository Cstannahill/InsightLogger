using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Fingerprinting;

namespace InsightLogger.Infrastructure.Parsing.Python;

public sealed class PythonTracebackParser : IDiagnosticParser
{
    private static readonly Regex FramePattern = new(
        @"^\s*File ""(?<file>.+?)"", line (?<line>\d+)(?:, in (?<function>.+))?\s*$",
        RegexOptions.Compiled);

    private static readonly Regex ExceptionLinePattern = new(
        @"^(?<type>[A-Za-z_][A-Za-z0-9_\.]*)(?::\s*(?<message>.*))?$",
        RegexOptions.Compiled);

    private static readonly HashSet<string> KnownNonSuffixExceptionTypes = new(StringComparer.Ordinal)
    {
        "SystemExit",
        "KeyboardInterrupt",
        "GeneratorExit",
        "StopIteration",
        "StopAsyncIteration"
    };

    private readonly DeterministicDiagnosticFingerprintGenerator _fingerprintGenerator = new();

    public string Name => "python-traceback-parser-v1";

    public ToolKind ToolKind => ToolKind.Python;

    public bool CanHandle(ParseDiagnosticsRequest request)
    {
        if (request.ToolHint == ToolKind.Python)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return false;
        }

        if (request.Content.Contains("Traceback (most recent call last):", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var lastNonEmptyLine = GetLastNonEmptyLine(request.Content);
        return !string.IsNullOrWhiteSpace(lastNonEmptyLine)
               && TryParseExceptionLine(lastNonEmptyLine!, out _, out _);
    }

    public Task<ParseDiagnosticsResult> ParseAsync(ParseDiagnosticsRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Content);

        var segments = Segment(request.Content);
        if (segments.Count == 0 && TryParseSegment(request.Content, out _))
        {
            segments = new List<string> { request.Content.Trim() };
        }

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
            ToolKind: ToolKind.Python,
            ParserName: Name,
            ParseConfidence: parseConfidence,
            Diagnostics: diagnostics,
            TotalSegments: segments.Count,
            ParsedSegments: diagnostics.Count,
            UnparsedSegments: unparsedSegments));
    }

    private static List<string> Segment(string content)
    {
        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var segments = new List<string>();
        StringBuilder? current = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (line.StartsWith("Traceback (most recent call last):", StringComparison.OrdinalIgnoreCase))
            {
                FlushCurrent();
                current = new StringBuilder();
                current.Append(line);
                continue;
            }

            if (current is null)
            {
                continue;
            }

            current.AppendLine();
            current.Append(line);
        }

        FlushCurrent();
        return segments;

        void FlushCurrent()
        {
            if (current is null || current.Length == 0)
            {
                return;
            }

            segments.Add(current.ToString().Trim());
            current = null;
        }
    }

    private bool TryParseSegment(string segment, out DiagnosticRecord? diagnostic)
    {
        var lines = segment.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var exceptionIndex = -1;
        string? exceptionType = null;
        string? exceptionMessage = null;

        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var candidate = lines[i].Trim();
            if (!TryParseExceptionLine(candidate, out exceptionType, out exceptionMessage))
            {
                continue;
            }

            exceptionIndex = i;
            break;
        }

        if (exceptionIndex < 0 || string.IsNullOrWhiteSpace(exceptionType))
        {
            diagnostic = null;
            return false;
        }

        var frameCount = 0;
        string? filePath = null;
        int? lineNumber = null;
        string? functionName = null;

        for (var i = 0; i < lines.Length; i++)
        {
            var frameMatch = FramePattern.Match(lines[i]);
            if (!frameMatch.Success)
            {
                continue;
            }

            frameCount++;
            filePath = PythonDiagnosticNormalizer.NormalizePath(frameMatch.Groups["file"].Value);
            lineNumber = ParseNullableInt(frameMatch.Groups["line"].Value);
            functionName = frameMatch.Groups["function"].Success
                ? PythonDiagnosticNormalizer.NormalizeWhitespace(frameMatch.Groups["function"].Value)
                : null;
        }

        var column = ResolveColumn(lines, exceptionIndex, exceptionType!);
        var message = string.IsNullOrWhiteSpace(exceptionMessage)
            ? exceptionType!
            : PythonDiagnosticNormalizer.NormalizeWhitespace(exceptionMessage);
        var normalizedMessage = PythonDiagnosticNormalizer.NormalizeMessage(exceptionType!, exceptionMessage);
        var category = PythonDiagnosticClassifier.Classify(exceptionType, normalizedMessage);
        var location = new DiagnosticLocation(
            FilePath: string.IsNullOrWhiteSpace(filePath) ? null : filePath,
            Line: lineNumber,
            Column: column);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["frameCount"] = frameCount.ToString()
        };

        if (!string.IsNullOrWhiteSpace(functionName))
        {
            metadata["function"] = functionName;
        }

        var record = new DiagnosticRecord(
            toolKind: ToolKind.Python,
            severity: Severity.Error,
            message: message,
            rawSnippet: segment,
            source: "python",
            code: exceptionType,
            normalizedMessage: normalizedMessage,
            location: location,
            category: category,
            metadata: metadata);

        diagnostic = record.WithFingerprint(_fingerprintGenerator.Generate(record));
        return true;
    }

    private static int? ResolveColumn(string[] lines, int exceptionIndex, string exceptionType)
    {
        if (!string.Equals(exceptionType, "SyntaxError", StringComparison.Ordinal)
            && !string.Equals(exceptionType, "IndentationError", StringComparison.Ordinal)
            && !string.Equals(exceptionType, "TabError", StringComparison.Ordinal))
        {
            return null;
        }

        for (var i = exceptionIndex - 1; i >= 0 && i >= exceptionIndex - 4; i--)
        {
            var caretIndex = lines[i].IndexOf('^');
            if (caretIndex >= 0)
            {
                return caretIndex + 1;
            }
        }

        return null;
    }

    private static bool TryParseExceptionLine(string line, out string? exceptionType, out string? exceptionMessage)
    {
        exceptionType = null;
        exceptionMessage = null;

        if (string.IsNullOrWhiteSpace(line)
            || line.StartsWith("Traceback", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("File ", StringComparison.Ordinal)
            || line.StartsWith("During handling", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("The above exception", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("^", StringComparison.Ordinal))
        {
            return false;
        }

        var match = ExceptionLinePattern.Match(line);
        if (!match.Success)
        {
            return false;
        }

        var type = match.Groups["type"].Value.Trim();
        if (!LooksLikePythonExceptionType(type))
        {
            return false;
        }

        exceptionType = type;
        exceptionMessage = match.Groups["message"].Success ? match.Groups["message"].Value.Trim() : null;
        return true;
    }

    private static bool LooksLikePythonExceptionType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.EndsWith("Error", StringComparison.Ordinal)
               || value.EndsWith("Exception", StringComparison.Ordinal)
               || value.EndsWith("Warning", StringComparison.Ordinal)
               || KnownNonSuffixExceptionTypes.Contains(value);
    }

    private static int? ParseNullableInt(string value) =>
        int.TryParse(value, out var parsed) ? parsed : null;

    private static string? GetLastNonEmptyLine(string content)
    {
        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                return lines[i].Trim();
            }
        }

        return null;
    }
}
