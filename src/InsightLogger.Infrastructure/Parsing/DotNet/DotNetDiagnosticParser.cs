using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Fingerprinting;

namespace InsightLogger.Infrastructure.Parsing.DotNet;

public sealed class DotNetDiagnosticParser : IDiagnosticParser
{
    private static readonly Regex DiagnosticStartRegex = new(
        @"(?:(?:^|\s)(?:error|warning|info|fatal)\s+(?:CS|MSB|NU|NETSDK)\d+\s*:)|(?:\([^\)]*\):\s*(?:error|warning|info|fatal)\s+(?:CS|MSB|NU|NETSDK)\d+\s*:)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LocationPattern = new(
        @"^(?<file>.+?)\((?<line>\d+),(?<column>\d+)(?:,(?<endLine>\d+),(?<endColumn>\d+))?\):\s*(?<severity>error|warning|info|fatal)\s+(?<code>[A-Z]{2,}\d{3,7})\s*:\s*(?<message>.+?)(?:\s+\[(?<project>[^\]]+)\])?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex WithoutLocationPattern = new(
        @"^(?:(?<source>.+?)\s*:\s*)?(?<severity>error|warning|info|fatal)\s+(?<code>[A-Z]{2,}\d{3,7})\s*:\s*(?<message>.+?)(?:\s+\[(?<project>[^\]]+)\])?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly DeterministicDiagnosticFingerprintGenerator _fingerprintGenerator = new();

    public string Name => "dotnet-diagnostic-parser-v1";

    public ToolKind ToolKind => ToolKind.DotNet;

    public bool CanHandle(ParseDiagnosticsRequest request)
    {
        if (request.ToolHint == ToolKind.DotNet)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return false;
        }

        return request.Content.Contains(" error CS", StringComparison.OrdinalIgnoreCase)
               || request.Content.Contains(" warning CS", StringComparison.OrdinalIgnoreCase)
               || request.Content.Contains(" error MSB", StringComparison.OrdinalIgnoreCase)
               || request.Content.Contains(" error NETSDK", StringComparison.OrdinalIgnoreCase)
               || request.Content.Contains(" error NU", StringComparison.OrdinalIgnoreCase)
               || request.Content.Contains(" warning NU", StringComparison.OrdinalIgnoreCase)
               || Regex.IsMatch(request.Content, @"\b(CS|MSB|NETSDK|NU)\d{3,7}\b", RegexOptions.IgnoreCase);
    }

    public Task<ParseDiagnosticsResult> ParseAsync(
        ParseDiagnosticsRequest request,
        CancellationToken cancellationToken = default)
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
            ToolKind: ToolKind.DotNet,
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
                FlushCurrentSegment();
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
                current.Append(line.Trim());
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

    private static bool LooksLikeDiagnosticStart(string line) =>
        DiagnosticStartRegex.IsMatch(line)
        || LocationPattern.IsMatch(line)
        || WithoutLocationPattern.IsMatch(line);

    private static bool IsContinuationLine(string line)
    {
        if (LooksLikeDiagnosticStart(line))
        {
            return false;
        }

        if (line.StartsWith("Build ", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("Time Elapsed", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(line, @"^\d+\s+Warning\(s\)", RegexOptions.IgnoreCase)
            || Regex.IsMatch(line, @"^\d+\s+Error\(s\)", RegexOptions.IgnoreCase)
            || line.StartsWith("Done Building Project", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return char.IsWhiteSpace(line[0])
               || line.StartsWith("Additional information:", StringComparison.OrdinalIgnoreCase)
               || line.StartsWith("See ", StringComparison.OrdinalIgnoreCase)
               || line.StartsWith("at ", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryParseSegment(string segment, out DiagnosticRecord? diagnostic)
    {
        var match = LocationPattern.Match(segment);
        if (match.Success)
        {
            diagnostic = BuildDiagnosticFromLocationMatch(match, segment);
            return true;
        }

        match = WithoutLocationPattern.Match(segment);
        if (match.Success)
        {
            diagnostic = BuildDiagnosticFromSimpleMatch(match, segment);
            return true;
        }

        diagnostic = null;
        return false;
    }

    private DiagnosticRecord BuildDiagnosticFromLocationMatch(Match match, string segment)
    {
        var filePath = DotNetDiagnosticNormalizer.NormalizePath(match.Groups["file"].Value);
        var message = DotNetDiagnosticNormalizer.NormalizeWhitespace(match.Groups["message"].Value);
        var normalizedMessage = DotNetDiagnosticNormalizer.NormalizeMessage(message);
        var code = match.Groups["code"].Value.Trim();
        var severity = ParseSeverity(match.Groups["severity"].Value);
        var category = DotNetDiagnosticClassifier.Classify(code, normalizedMessage);
        var location = new DiagnosticLocation(
            FilePath: string.IsNullOrWhiteSpace(filePath) ? null : filePath,
            Line: ParseNullableInt(match.Groups["line"].Value),
            Column: ParseNullableInt(match.Groups["column"].Value),
            EndLine: ParseNullableInt(match.Groups["endLine"].Value),
            EndColumn: ParseNullableInt(match.Groups["endColumn"].Value));

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (match.Groups["project"].Success)
        {
            metadata["project"] = DotNetDiagnosticNormalizer.NormalizePath(match.Groups["project"].Value);
        }

        var source = ResolveSource(code);

        var diagnostic = new DiagnosticRecord(
            toolKind: ToolKind.DotNet,
            severity: severity,
            message: message,
            rawSnippet: segment,
            source: source,
            code: code,
            normalizedMessage: normalizedMessage,
            location: location,
            category: category,
            metadata: metadata);

        return diagnostic.WithFingerprint(_fingerprintGenerator.Generate(diagnostic));
    }

    private DiagnosticRecord BuildDiagnosticFromSimpleMatch(Match match, string segment)
    {
        var message = DotNetDiagnosticNormalizer.NormalizeWhitespace(match.Groups["message"].Value);
        var normalizedMessage = DotNetDiagnosticNormalizer.NormalizeMessage(message);
        var code = match.Groups["code"].Value.Trim();
        var severity = ParseSeverity(match.Groups["severity"].Value);
        var category = DotNetDiagnosticClassifier.Classify(code, normalizedMessage);
        var sourceCandidate = match.Groups["source"].Success ? match.Groups["source"].Value.Trim() : null;
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        DiagnosticLocation? location = null;

        if (match.Groups["project"].Success)
        {
            metadata["project"] = DotNetDiagnosticNormalizer.NormalizePath(match.Groups["project"].Value);
        }

        if (!string.IsNullOrWhiteSpace(sourceCandidate)
            && (sourceCandidate.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                || sourceCandidate.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
                || sourceCandidate.EndsWith(".targets", StringComparison.OrdinalIgnoreCase)
                || sourceCandidate.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)))
        {
            location = new DiagnosticLocation(DotNetDiagnosticNormalizer.NormalizePath(sourceCandidate), null, null);
        }

        var source = ResolveSource(code, sourceCandidate);

        var diagnostic = new DiagnosticRecord(
            toolKind: ToolKind.DotNet,
            severity: severity,
            message: message,
            rawSnippet: segment,
            source: source,
            code: code,
            normalizedMessage: normalizedMessage,
            location: location,
            category: category,
            metadata: metadata);

        return diagnostic.WithFingerprint(_fingerprintGenerator.Generate(diagnostic));
    }

    private static string ResolveSource(string code, string? explicitSource = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitSource)
            && !explicitSource.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            && !explicitSource.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
            && !explicitSource.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
            && !explicitSource.EndsWith(".targets", StringComparison.OrdinalIgnoreCase))
        {
            return explicitSource.Trim();
        }

        return code.ToUpperInvariant() switch
        {
            var c when c.StartsWith("CS", StringComparison.OrdinalIgnoreCase) => "Roslyn",
            var c when c.StartsWith("MSB", StringComparison.OrdinalIgnoreCase) => "MSBuild",
            var c when c.StartsWith("NU", StringComparison.OrdinalIgnoreCase) => "NuGet",
            var c when c.StartsWith("NETSDK", StringComparison.OrdinalIgnoreCase) => ".NET SDK",
            _ => ".NET"
        };
    }

    private static int? ParseNullableInt(string value) =>
        int.TryParse(value, out var parsed) ? parsed : null;

    private static Severity ParseSeverity(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "info" => Severity.Info,
            "warning" => Severity.Warning,
            "error" => Severity.Error,
            "fatal" => Severity.Fatal,
            _ => Severity.Unknown
        };
}
