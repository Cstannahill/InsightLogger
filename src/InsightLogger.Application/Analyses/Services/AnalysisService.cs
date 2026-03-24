using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Ai;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Application.Abstractions.Telemetry;
using InsightLogger.Application.Analyses.Commands;
using InsightLogger.Application.Analyses.Persistence;
using InsightLogger.Application.Logging;
using InsightLogger.Application.Rules.Services;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace InsightLogger.Application.Analyses.Services;

public sealed class AnalysisService : IAnalysisService
{
    private readonly IToolDetector _toolDetector;
    private readonly IDiagnosticParserCoordinator _parserCoordinator;
    private readonly DiagnosticGroupingService _groupingService;
    private readonly AnalysisNarrativeFactory _narrativeFactory;
    private readonly RootCauseRankingService _rankingService;
    private readonly RuleMatchingService _ruleMatchingService;
    private readonly AnalysisPersistenceService? _analysisPersistenceService;
    private readonly IAiExplanationEnricher? _aiExplanationEnricher;
    private readonly IAiRootCauseNarrativeGenerator? _aiRootCauseNarrativeGenerator;
    private readonly int _maxContentLength;
    private readonly IInsightLoggerTelemetry? _telemetry;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(
        IToolDetector toolDetector,
        IDiagnosticParserCoordinator parserCoordinator,
        DiagnosticGroupingService groupingService,
        AnalysisNarrativeFactory narrativeFactory,
        RootCauseRankingService rankingService,
        RuleMatchingService ruleMatchingService,
        AnalysisPersistenceService? analysisPersistenceService = null,
        IAiExplanationEnricher? aiExplanationEnricher = null,
        IAiRootCauseNarrativeGenerator? aiRootCauseNarrativeGenerator = null,
        int maxContentLength = 250_000,
        IInsightLoggerTelemetry? telemetry = null,
        ILogger<AnalysisService>? logger = null)
    {
        _toolDetector = toolDetector;
        _parserCoordinator = parserCoordinator;
        _groupingService = groupingService;
        _narrativeFactory = narrativeFactory;
        _rankingService = rankingService;
        _ruleMatchingService = ruleMatchingService;
        _analysisPersistenceService = analysisPersistenceService;
        _aiExplanationEnricher = aiExplanationEnricher;
        _aiRootCauseNarrativeGenerator = aiRootCauseNarrativeGenerator;
        _maxContentLength = maxContentLength;
        _telemetry = telemetry;
        _logger = logger ?? NullLogger<AnalysisService>.Instance;
    }

    public async Task<AnalysisResult> AnalyzeAsync(AnalyzeInputCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Content);

        if (command.Content.Length > _maxContentLength)
        {
            _logger.LogWarning(
                "Analysis request rejected because content length exceeded the configured maximum. ContentLength={ContentLength} MaxContentLength={MaxContentLength} CorrelationId={CorrelationId}.",
                command.Content.Length,
                _maxContentLength,
                command.CorrelationId);

            throw new ArgumentOutOfRangeException(nameof(command), $"Content length exceeds the configured maximum of {_maxContentLength} characters.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var contentLineCount = CountLines(command.Content);
        var contentHashPrefix = ComputeContentHashPrefix(command.Content);
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["analysisId"] = null,
            ["correlationId"] = command.CorrelationId,
            ["inputType"] = command.InputType.ToString(),
            ["toolHint"] = command.ToolHint?.ToString(),
            ["persistRequested"] = command.Persist,
            ["useAiEnrichment"] = command.UseAiEnrichment,
            ["useAiRootCauseNarrative"] = command.UseAiRootCauseNarrative,
            ["contentHashPrefix"] = contentHashPrefix
        });

        _logger.LogInformation(
            "Starting analysis pipeline. InputType={InputType} ToolHint={ToolHint} ContentLength={ContentLength} ContentLineCount={ContentLineCount} ContentHashPrefix={ContentHashPrefix} PersistRequested={PersistRequested} UseAiEnrichment={UseAiEnrichment} UseAiRootCauseNarrative={UseAiRootCauseNarrative}.",
            command.InputType,
            command.ToolHint,
            command.Content.Length,
            contentLineCount,
            contentHashPrefix,
            command.Persist,
            command.UseAiEnrichment,
            command.UseAiRootCauseNarrative);

        using var activity = _telemetry?.StartAnalysisActivity(command.InputType.ToString(), command.CorrelationId);
        var stopwatch = Stopwatch.StartNew();

        ToolDetectionResult? toolDetection = null;
        DiagnosticParserCoordinatorResult? coordinatedParse = null;
        IReadOnlyList<DiagnosticRecord> diagnostics = Array.Empty<DiagnosticRecord>();
        IReadOnlyList<DiagnosticGroup> groups = Array.Empty<DiagnosticGroup>();
        IReadOnlyList<RootCauseCandidate> rootCauseCandidates = Array.Empty<RootCauseCandidate>();
        IReadOnlyList<AiProcessingMetadata> aiTasks = Array.Empty<AiProcessingMetadata>();
        var persistenceSucceeded = !command.Persist;
        AnalysisResult? result = null;
        string? failureReason = null;

        try
        {
            toolDetection = await _toolDetector.DetectAsync(command.Content, command.ToolHint, cancellationToken);
            coordinatedParse = await _parserCoordinator.ParseAsync(command.Content, command.InputType, toolDetection.ToolKind, command.CorrelationId, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            diagnostics = coordinatedParse.ParseResult?.Diagnostics ?? Array.Empty<DiagnosticRecord>();
            groups = _groupingService.Group(diagnostics);
            rootCauseCandidates = _rankingService.Rank(diagnostics, groups, coordinatedParse.ParseResult?.ParseConfidence ?? 0d);

            var ruleEvaluation = await _ruleMatchingService.EvaluateAsync(
                toolKind: toolDetection.ToolKind,
                diagnostics: diagnostics,
                groups: groups,
                currentCandidates: rootCauseCandidates,
                context: command.Context,
                cancellationToken: cancellationToken);

            rootCauseCandidates = ruleEvaluation.RootCauseCandidates;
            var summary = BuildSummary(diagnostics, groups, rootCauseCandidates);
            var narrative = command.InputType == InputType.BuildLog
                ? _narrativeFactory.Build(toolDetection.ToolKind, summary, groups, rootCauseCandidates)
                : null;

            var notes = coordinatedParse.FailureReason;
            var warnings = new List<string>();
            var mutableAiTasks = new List<AiProcessingMetadata>();

            if (command.UseAiEnrichment)
            {
                if (_aiExplanationEnricher is not null)
                {
                    var enrichedCandidates = await TryEnrichPrimaryCandidateAsync(
                        toolDetection.ToolKind,
                        rootCauseCandidates,
                        diagnostics,
                        command,
                        warnings,
                        cancellationToken);

                    rootCauseCandidates = enrichedCandidates.Candidates;
                    if (enrichedCandidates.Metadata is not null)
                    {
                        mutableAiTasks.Add(enrichedCandidates.Metadata);
                    }
                }
                else
                {
                    mutableAiTasks.Add(new AiProcessingMetadata(
                        Requested: true,
                        Status: "unavailable",
                        Reason: "AI explanation enrichment service is not configured.",
                        Feature: "explanation-enrichment"));

                    warnings.Add("AI explanation enrichment was requested but could not be completed. Deterministic analysis was returned instead.");
                }
            }

            if (command.UseAiRootCauseNarrative)
            {
                if (narrative is null)
                {
                    mutableAiTasks.Add(new AiProcessingMetadata(
                        Requested: true,
                        Status: "skipped",
                        Reason: "No grouped build-log narrative target was available.",
                        Feature: "root-cause-narrative"));
                }
                else
                {
                    var enrichedNarrative = await TryEnrichNarrativeAsync(
                        toolDetection.ToolKind,
                        summary,
                        groups,
                        rootCauseCandidates,
                        narrative,
                        command,
                        warnings,
                        cancellationToken);

                    narrative = enrichedNarrative.Narrative;
                    if (enrichedNarrative.Metadata is not null)
                    {
                        mutableAiTasks.Add(enrichedNarrative.Metadata);
                    }
                }
            }

            aiTasks = mutableAiTasks;

            var processing = CreateProcessingMetadata(
                aiTasks,
                durationMs: (int)Math.Max(1, stopwatch.ElapsedMilliseconds),
                parser: coordinatedParse.SelectedParserName,
                correlationId: command.CorrelationId,
                toolDetectionConfidence: toolDetection.Confidence,
                parseConfidence: coordinatedParse.ParseResult?.ParseConfidence ?? 0d,
                unparsedSegmentCount: coordinatedParse.ParseResult?.UnparsedSegments?.Count ?? 0,
                notes: notes);

            result = new AnalysisResult(
                inputType: command.InputType,
                toolDetected: toolDetection.ToolKind,
                summary: summary,
                diagnostics: diagnostics,
                groups: groups,
                rootCauseCandidates: rootCauseCandidates,
                matchedRules: ruleEvaluation.Matches,
                narrative: narrative,
                processing: processing,
                warnings: warnings);

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
                    ruleEvaluation.Matches,
                    narrative,
                    ruleEvaluation.Applications,
                    processing,
                    warnings,
                    cancellationToken);

                persistenceSucceeded = persistResult.Persisted;

                if (!persistResult.Persisted && !string.IsNullOrWhiteSpace(persistResult.FailureReason))
                {
                    notes = AppendNote(notes, persistResult.FailureReason);
                    processing = CreateProcessingMetadata(
                        aiTasks,
                        durationMs: processing.DurationMs,
                        parser: processing.Parser,
                        correlationId: processing.CorrelationId,
                        toolDetectionConfidence: processing.ToolDetectionConfidence,
                        parseConfidence: processing.ParseConfidence,
                        unparsedSegmentCount: processing.UnparsedSegmentCount,
                        notes: notes);

                    result = result.WithProcessing(processing);
                }
            }
            else if (command.Persist)
            {
                persistenceSucceeded = false;
            }

            _logger.LogInformation(
                "Completed analysis pipeline. AnalysisId={AnalysisId} ToolDetected={ToolDetected} Parser={Parser} Diagnostics={DiagnosticCount} Groups={GroupCount} Candidates={CandidateCount} MatchedRules={MatchedRuleCount} UsedAi={UsedAi} DurationMs={DurationMs}.",
                result.AnalysisId,
                result.ToolDetected,
                result.Processing.Parser,
                result.Diagnostics.Count,
                result.Groups.Count,
                result.RootCauseCandidates.Count,
                result.MatchedRules.Count,
                result.Processing.UsedAi,
                result.Processing.DurationMs);

            activity?.SetTag("insightlogger.tool_detected", result.ToolDetected.ToString());
            activity?.SetTag("insightlogger.parser", result.Processing.Parser ?? "none");
            activity?.SetTag("insightlogger.diagnostics_count", result.Diagnostics.Count);
            activity?.SetTag("insightlogger.group_count", result.Groups.Count);
            activity?.SetTag("insightlogger.root_cause_candidate_count", result.RootCauseCandidates.Count);
            activity?.SetTag("insightlogger.used_ai", result.Processing.UsedAi);
            activity?.SetTag("insightlogger.persisted", !command.Persist || persistenceSucceeded);

            return result;
        }
        catch (Exception ex)
        {
            failureReason = ex.GetType().Name;
            activity?.SetTag("insightlogger.exception_type", ex.GetType().Name);
            _logger.LogError(
                "Analysis pipeline failed. ExceptionType={ExceptionType} ExceptionMessage={ExceptionMessage}.",
                ex.GetType().Name,
                LogRedactor.Redact(ex.Message));
            throw;
        }
        finally
        {
            stopwatch.Stop();

            if (_telemetry is not null)
            {
                var parserName = result?.Processing.Parser ?? coordinatedParse?.SelectedParserName;
                var parseSucceeded = coordinatedParse?.Success == true && diagnostics.Count > 0;
                var aiRequested = command.UseAiEnrichment || command.UseAiRootCauseNarrative;
                var aiCompleted = aiTasks.Any(static task => string.Equals(task.Status, "completed", StringComparison.OrdinalIgnoreCase));
                var isUnmatched = (result?.ToolDetected ?? toolDetection?.ToolKind ?? ToolKind.Unknown) == ToolKind.Unknown || diagnostics.Count == 0 || rootCauseCandidates.Count == 0;

                activity?.SetTag("insightlogger.analysis_succeeded", result is not null);
                activity?.SetTag("insightlogger.duration_ms", (int)Math.Max(1, stopwatch.ElapsedMilliseconds));
                activity?.SetTag("insightlogger.parse_succeeded", parseSucceeded);
                activity?.SetTag("insightlogger.unmatched", isUnmatched);

                _telemetry.RecordAnalysisCompleted(new AnalysisTelemetryEvent(
                    InputType: command.InputType.ToString(),
                    ToolDetected: (result?.ToolDetected ?? toolDetection?.ToolKind ?? ToolKind.Unknown).ToString(),
                    Parser: parserName,
                    Succeeded: result is not null,
                    ParseSucceeded: parseSucceeded,
                    AiRequested: aiRequested,
                    AiCompleted: aiCompleted,
                    PersistenceRequested: command.Persist,
                    PersistenceSucceeded: persistenceSucceeded,
                    IsUnmatched: isUnmatched,
                    DurationMs: (int)Math.Max(1, stopwatch.ElapsedMilliseconds),
                    DiagnosticsCount: diagnostics.Count,
                    GroupCount: groups.Count,
                    RootCauseCandidateCount: rootCauseCandidates.Count,
                    UnparsedSegmentCount: coordinatedParse?.ParseResult?.UnparsedSegments?.Count ?? 0,
                    Fingerprints: diagnostics
                        .Where(static diagnostic => diagnostic.Fingerprint is not null)
                        .Select(static diagnostic => diagnostic.Fingerprint!.Value.Value)
                        .ToArray(),
                    CorrelationId: command.CorrelationId,
                    FailureReason: failureReason));
            }
        }
    }

    private async Task<(IReadOnlyList<RootCauseCandidate> Candidates, AiProcessingMetadata? Metadata)> TryEnrichPrimaryCandidateAsync(
        ToolKind toolKind,
        IReadOnlyList<RootCauseCandidate> rootCauseCandidates,
        IReadOnlyList<DiagnosticRecord> diagnostics,
        AnalyzeInputCommand command,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var primaryCandidate = rootCauseCandidates.FirstOrDefault();
        if (primaryCandidate is null || string.IsNullOrWhiteSpace(primaryCandidate.Explanation))
        {
            return (rootCauseCandidates, new AiProcessingMetadata(
                Requested: true,
                Status: "skipped",
                Reason: "No eligible root cause candidate was available for AI enrichment.",
                Feature: "explanation-enrichment"));
        }

        var relatedDiagnostic = !string.IsNullOrWhiteSpace(primaryCandidate.DiagnosticId)
            ? diagnostics.FirstOrDefault(d => string.Equals(d.Id, primaryCandidate.DiagnosticId, StringComparison.Ordinal))
            : diagnostics.FirstOrDefault(d => d.IsPrimaryCandidate) ?? diagnostics.FirstOrDefault();

        var enrichment = await _aiExplanationEnricher!.EnrichAsync(
            new ExplanationEnrichmentRequest(
                Tool: toolKind.ToString(),
                DiagnosticCode: relatedDiagnostic?.Code,
                Category: relatedDiagnostic?.Category.ToString(),
                Title: primaryCandidate.Title,
                Explanation: primaryCandidate.Explanation,
                LikelyCauses: primaryCandidate.LikelyCauses,
                SuggestedFixes: primaryCandidate.SuggestedFixes,
                Signals: primaryCandidate.Signals,
                NormalizedMessage: relatedDiagnostic?.NormalizedMessage,
                Context: command.Context,
                CorrelationId: command.CorrelationId),
            cancellationToken);

        var metadata = new AiProcessingMetadata(
            Requested: true,
            Provider: enrichment.Provider,
            Model: enrichment.Model,
            Status: enrichment.Status,
            FallbackUsed: enrichment.FallbackUsed,
            Reason: enrichment.Reason,
            Feature: "explanation-enrichment");

        if (!enrichment.Success || string.IsNullOrWhiteSpace(enrichment.Explanation))
        {
            warnings.Add("AI explanation enrichment was requested but could not be completed. Deterministic analysis was returned instead.");
            return (rootCauseCandidates, metadata);
        }

        var updatedSuggestedFixes = enrichment.SuggestedFixes.Count > 0
            ? enrichment.SuggestedFixes
            : primaryCandidate.SuggestedFixes;

        var updatedLikelyCauses = enrichment.LikelyCauses.Count > 0
            ? enrichment.LikelyCauses
            : primaryCandidate.LikelyCauses;

        var updated = rootCauseCandidates
            .Select(candidate => string.Equals(candidate.Fingerprint.Value, primaryCandidate.Fingerprint.Value, StringComparison.Ordinal) &&
                                 string.Equals(candidate.DiagnosticId, primaryCandidate.DiagnosticId, StringComparison.Ordinal)
                ? candidate with
                {
                    Explanation = enrichment.Explanation,
                    SuggestedFixes = updatedSuggestedFixes,
                    LikelyCauses = updatedLikelyCauses
                }
                : candidate)
            .ToArray();

        return (updated, metadata);
    }

    private async Task<(AnalysisNarrative Narrative, AiProcessingMetadata? Metadata)> TryEnrichNarrativeAsync(
        ToolKind toolKind,
        AnalysisSummary summary,
        IReadOnlyList<DiagnosticGroup> groups,
        IReadOnlyList<RootCauseCandidate> rootCauseCandidates,
        AnalysisNarrative narrative,
        AnalyzeInputCommand command,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        if (_aiRootCauseNarrativeGenerator is null)
        {
            warnings.Add("AI root-cause narrative generation was requested but could not be completed. Deterministic narrative was returned instead.");
            return (narrative, new AiProcessingMetadata(
                Requested: true,
                Status: "unavailable",
                Reason: "AI root-cause narrative service is not configured.",
                Feature: "root-cause-narrative"));
        }

        var enrichment = await _aiRootCauseNarrativeGenerator.GenerateAsync(
            new RootCauseNarrativeRequest(
                Tool: toolKind.ToString(),
                TotalDiagnostics: summary.TotalDiagnostics,
                GroupCount: summary.GroupCount,
                ErrorCount: summary.ErrorCount,
                WarningCount: summary.WarningCount,
                TopRootCauseTitles: rootCauseCandidates.Select(static candidate => candidate.Title).Take(3).ToArray(),
                DeterministicGroupSummaries: narrative.GroupSummaries,
                DeterministicNextSteps: narrative.RecommendedNextSteps,
                DeterministicSummary: narrative.Summary,
                Context: command.Context,
                CorrelationId: command.CorrelationId),
            cancellationToken);

        var metadata = new AiProcessingMetadata(
            Requested: true,
            Provider: enrichment.Provider,
            Model: enrichment.Model,
            Status: enrichment.Status,
            FallbackUsed: enrichment.FallbackUsed,
            Reason: enrichment.Reason,
            Feature: "root-cause-narrative");

        if (!enrichment.Success || string.IsNullOrWhiteSpace(enrichment.Summary))
        {
            warnings.Add("AI root-cause narrative generation was requested but could not be completed. Deterministic narrative was returned instead.");
            return (narrative, metadata);
        }

        return (
            narrative.WithAi(
                summary: enrichment.Summary,
                groupSummaries: enrichment.GroupSummaries.Count > 0 ? enrichment.GroupSummaries : narrative.GroupSummaries,
                recommendedNextSteps: enrichment.RecommendedNextSteps.Count > 0 ? enrichment.RecommendedNextSteps : narrative.RecommendedNextSteps,
                provider: enrichment.Provider ?? string.Empty,
                model: enrichment.Model ?? string.Empty,
                fallbackUsed: enrichment.FallbackUsed,
                reason: enrichment.Reason),
            metadata);
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

    private static ProcessingMetadata CreateProcessingMetadata(
        IReadOnlyList<AiProcessingMetadata> aiTasks,
        int durationMs,
        string? parser,
        string? correlationId,
        double toolDetectionConfidence,
        double parseConfidence,
        int unparsedSegmentCount,
        string? notes)
    {
        var aiSummary = aiTasks.Count == 1 ? aiTasks[0] : null;

        return new ProcessingMetadata(
            UsedAi: aiTasks.Any(static task => string.Equals(task.Status, "completed", StringComparison.OrdinalIgnoreCase)),
            DurationMs: durationMs,
            Parser: parser,
            CorrelationId: correlationId,
            ToolDetectionConfidence: toolDetectionConfidence,
            ParseConfidence: parseConfidence,
            UnparsedSegmentCount: unparsedSegmentCount,
            Notes: notes,
            Ai: aiSummary,
            AiTasks: aiTasks);
    }

    private static string AppendNote(string? existing, string addition)
    {
        if (string.IsNullOrWhiteSpace(existing))
        {
            return addition;
        }

        return $"{existing}; {addition}";
    }

    private static int CountLines(string content)
        => string.IsNullOrEmpty(content)
            ? 0
            : content.Count(static ch => ch == '\n') + 1;

    private static string ComputeContentHashPrefix(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        var hash = Convert.ToHexString(bytes).ToLowerInvariant();
        return hash[..Math.Min(12, hash.Length)];
    }
}
