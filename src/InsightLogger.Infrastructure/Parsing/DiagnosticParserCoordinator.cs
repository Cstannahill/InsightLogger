using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Infrastructure.Parsing;

public sealed class DiagnosticParserCoordinator : IDiagnosticParserCoordinator
{
    private readonly IReadOnlyList<IDiagnosticParser> _parsers;

    public DiagnosticParserCoordinator(IEnumerable<IDiagnosticParser> parsers)
    {
        _parsers = parsers?.ToList() ?? throw new ArgumentNullException(nameof(parsers));
    }

    public async Task<DiagnosticParserCoordinatorResult> ParseAsync(
        string content,
        InputType inputType,
        ToolKind detectedTool,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var request = new ParseDiagnosticsRequest(
            Content: content,
            InputType: inputType,
            ToolHint: detectedTool == ToolKind.Unknown ? null : detectedTool,
            CaptureUnparsedSegments: true,
            Metadata: string.IsNullOrWhiteSpace(correlationId)
                ? null
                : new Dictionary<string, string>(StringComparer.Ordinal) { ["correlationId"] = correlationId });

        var parser = SelectParser(request, detectedTool);
        if (parser is null)
        {
            return new DiagnosticParserCoordinatorResult(
                ToolKind: detectedTool,
                SelectedParserName: null,
                ParseResult: null,
                FailureReason: detectedTool == ToolKind.Unknown
                    ? "No parser could be selected because tool detection returned unknown."
                    : $"No registered parser could handle tool '{detectedTool}'.");
        }

        var parseResult = await parser.ParseAsync(request, cancellationToken);
        return new DiagnosticParserCoordinatorResult(
            ToolKind: parseResult.ToolKind,
            SelectedParserName: parser.Name,
            ParseResult: parseResult);
    }

    private IDiagnosticParser? SelectParser(ParseDiagnosticsRequest request, ToolKind detectedTool)
    {
        if (detectedTool != ToolKind.Unknown)
        {
            var exact = _parsers.FirstOrDefault(p => p.ToolKind == detectedTool && p.CanHandle(request));
            if (exact is not null)
            {
                return exact;
            }
        }

        return _parsers.FirstOrDefault(p => p.CanHandle(request));
    }
}
