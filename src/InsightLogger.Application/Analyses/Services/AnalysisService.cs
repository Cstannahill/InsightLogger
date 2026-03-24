using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Application.Analyses.Commands;
using InsightLogger.Application.Analyses.Persistence;
using InsightLogger.Application.Rules.Services;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Analyses.Services;

public sealed class AnalysisService : IAnalysisService
{
    private readonly IToolDetector _toolDetector;
    private readonly IDiagnosticParserCoordinator _parserCoordinator;
    private readonly DiagnosticGroupingService _groupingService;
    private readonly RootCauseRankingService _rankingService;
    private readonly RuleMatchingService _ruleMatchingService;
    private readonly AnalysisPersistenceService? _analysisPersistenceService;
    private readonly int _maxContentLength;

    public AnalysisService(
        IToolDetector toolDetector,
        IDiagnosticParserCoordinator parserCoordinator,
        DiagnosticGroupingService groupingService,
        RootCauseRankingService rankingService,
        RuleMatchingService ruleMatchingService,
        AnalysisPersistenceService? analysisPersistenceService = null,
        int maxContentLength = 250_000)
    {
        _toolDetector = toolDetector;
        _parserCoordinator = parserCoordinator;
        _groupingService = groupingService;
        _rankingService = rankingService;
        _ruleMatchingService = ruleMatchingService;
        _analysisPersistenceService = analysisPersistenceService;
        _maxContentLength = maxContentLength;
    }

    public async Task<AnalysisResult> AnalyzeAsync(AnalyzeInputCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Content);

        if (command.Content.Length > _maxContentLength)
        {
            throw new ArgumentOutOfRangeException(nameof(command), $"Content length exceeds the configured maximum of {_maxContentLength} characters.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var stopwatch = Stopwatch.StartNew();
        var toolDetection = await _toolDetector.DetectAsync(command.Content, command.ToolHint, cancellationToken);
        var coordinatedParse = await _parserCoordinator.ParseAsync(command.Content, command.InputType, toolDetection.ToolKind, command.CorrelationId, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = coordinatedParse.ParseResult?.Diagnostics ?? Array.Empty<DiagnosticRecord>();
        var groups = _groupingService.Group(diagnostics);
        var rootCauseCandidates = _rankingService.Rank(diagnostics, groups, coordinatedParse.ParseResult?.ParseConfidence ?? 0d);

        var ruleEvaluation = await _ruleMatchingService.EvaluateAsync(
            toolKind: toolDetection.ToolKind,
            diagnostics: diagnostics,
            groups: groups,
            currentCandidates: rootCauseCandidates,
            context: command.Context,
            cancellationToken: cancellationToken);

        rootCauseCandidates = ruleEvaluation.RootCauseCandidates;
        var summary = BuildSummary(diagnostics, groups, rootCauseCandidates);

        stopwatch.Stop();

        var notes = coordinatedParse.FailureReason;
        var processing = new ProcessingMetadata(
            UsedAi: false,
            DurationMs: (int)Math.Max(1, stopwatch.ElapsedMilliseconds),
            Parser: coordinatedParse.SelectedParserName,
            CorrelationId: command.CorrelationId,
            ToolDetectionConfidence: toolDetection.Confidence,
            ParseConfidence: coordinatedParse.ParseResult?.ParseConfidence ?? 0d,
            UnparsedSegmentCount: coordinatedParse.ParseResult?.UnparsedSegments?.Count ?? 0,
            Notes: notes);

        var result = new AnalysisResult(
            inputType: command.InputType,
            toolDetected: toolDetection.ToolKind,
            summary: summary,
            diagnostics: diagnostics,
            groups: groups,
            rootCauseCandidates: rootCauseCandidates,
            matchedRules: ruleEvaluation.Matches,
            processing: processing);

        if (command.Persist && _analysisPersistenceService is not null)
        {
            var persistResult = await _analysisPersistenceService.TryPersistAsync(
                result.AnalysisId,
                command,
                toolDetection.ToolKind,
                summary,
                diagnostics,
                groups,
                rootCauseCandidates,
                ruleEvaluation.Applications,
                processing,
                cancellationToken);

            if (!persistResult.Persisted && !string.IsNullOrWhiteSpace(persistResult.FailureReason))
            {
                notes = AppendNote(notes, persistResult.FailureReason);
                processing = new ProcessingMetadata(
                    UsedAi: processing.UsedAi,
                    DurationMs: processing.DurationMs,
                    Parser: processing.Parser,
                    CorrelationId: processing.CorrelationId,
                    ToolDetectionConfidence: processing.ToolDetectionConfidence,
                    ParseConfidence: processing.ParseConfidence,
                    UnparsedSegmentCount: processing.UnparsedSegmentCount,
                    Notes: notes);

                result = result.WithProcessing(processing);
            }
        }

        return result;
    }

    private static AnalysisSummary BuildSummary(
        IReadOnlyList<DiagnosticRecord> diagnostics,
        IReadOnlyList<DiagnosticGroup> groups,
        IReadOnlyList<RootCauseCandidate> rootCauseCandidates)
    {
        var errorCount = diagnostics.Count(static d => d.Severity is Severity.Error or Severity.Fatal);
        var warningCount = diagnostics.Count(static d => d.Severity == Severity.Warning);

        return new AnalysisSummary(
            TotalDiagnostics: diagnostics.Count,
            GroupCount: groups.Count,
            PrimaryIssueCount: rootCauseCandidates.Count,
            ErrorCount: errorCount,
            WarningCount: warningCount);
    }

    private static string AppendNote(string? existing, string addition)
    {
        if (string.IsNullOrWhiteSpace(existing))
        {
            return addition;
        }

        return $"{existing}; {addition}";
    }
}
