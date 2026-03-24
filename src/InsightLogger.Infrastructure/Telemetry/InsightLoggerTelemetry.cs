using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using InsightLogger.Application.Abstractions.Telemetry;
using Microsoft.Extensions.Options;

namespace InsightLogger.Infrastructure.Telemetry;

public sealed class InsightLoggerTelemetry : IInsightLoggerTelemetry
{
    public const string MeterName = "InsightLogger.Telemetry";
    public const string ActivitySourceName = "InsightLogger.Analysis";

    private static readonly Meter SharedMeter = new(MeterName, "1.0.0");
    private static readonly ActivitySource SharedActivitySource = new(ActivitySourceName, "1.0.0");

    private readonly InsightLoggerTelemetryOptions _options;
    private readonly Counter<long> _analysisRequestsCounter;
    private readonly Counter<long> _analysisOutcomesCounter;
    private readonly Counter<long> _analysisAiCounter;
    private readonly Counter<long> _analysisPersistenceCounter;
    private readonly Histogram<double> _analysisDurationMsHistogram;
    private readonly Histogram<long> _analysisDiagnosticsHistogram;
    private readonly Counter<long> _httpRequestsCounter;
    private readonly Histogram<double> _httpDurationMsHistogram;
    private readonly ConcurrentDictionary<string, long> _toolSelections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _parserSelections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _fingerprints = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _httpMethods = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _httpStatusCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _httpRoutes = new(StringComparer.OrdinalIgnoreCase);

    private long _analysisTotalRequests;
    private long _analysisCompleted;
    private long _analysisFailed;
    private long _analysisParseFailures;
    private long _analysisAiRequested;
    private long _analysisAiCompleted;
    private long _analysisPersistenceFailures;
    private long _analysisUnmatched;
    private long _analysisTotalDurationMs;
    private long _analysisTotalDiagnostics;
    private long _httpTotalRequests;
    private long _httpTotalDurationMs;

    public InsightLoggerTelemetry(IOptions<InsightLoggerTelemetryOptions> options)
    {
        _options = options.Value ?? new InsightLoggerTelemetryOptions();

        _analysisRequestsCounter = SharedMeter.CreateCounter<long>("insightlogger.analysis.requests");
        _analysisOutcomesCounter = SharedMeter.CreateCounter<long>("insightlogger.analysis.outcomes");
        _analysisAiCounter = SharedMeter.CreateCounter<long>("insightlogger.analysis.ai");
        _analysisPersistenceCounter = SharedMeter.CreateCounter<long>("insightlogger.analysis.persistence");
        _analysisDurationMsHistogram = SharedMeter.CreateHistogram<double>("insightlogger.analysis.duration.ms");
        _analysisDiagnosticsHistogram = SharedMeter.CreateHistogram<long>("insightlogger.analysis.diagnostics.count");
        _httpRequestsCounter = SharedMeter.CreateCounter<long>("insightlogger.http.requests");
        _httpDurationMsHistogram = SharedMeter.CreateHistogram<double>("insightlogger.http.duration.ms");
    }

    public Activity? StartAnalysisActivity(string inputType, string? correlationId = null)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var activity = SharedActivitySource.StartActivity("analysis.execute", ActivityKind.Internal);
        activity?.SetTag("insightlogger.input_type", inputType);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            activity?.SetTag("insightlogger.correlation_id", correlationId);
        }

        return activity;
    }

    public void RecordAnalysisCompleted(AnalysisTelemetryEvent telemetryEvent)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);

        if (!_options.Enabled)
        {
            return;
        }

        Interlocked.Increment(ref _analysisTotalRequests);
        Interlocked.Add(ref _analysisTotalDurationMs, Math.Max(1, telemetryEvent.DurationMs));
        Interlocked.Add(ref _analysisTotalDiagnostics, Math.Max(0, telemetryEvent.DiagnosticsCount));

        if (telemetryEvent.Succeeded)
        {
            Interlocked.Increment(ref _analysisCompleted);
        }
        else
        {
            Interlocked.Increment(ref _analysisFailed);
        }

        if (!telemetryEvent.ParseSucceeded)
        {
            Interlocked.Increment(ref _analysisParseFailures);
        }

        if (telemetryEvent.AiRequested)
        {
            Interlocked.Increment(ref _analysisAiRequested);
        }

        if (telemetryEvent.AiCompleted)
        {
            Interlocked.Increment(ref _analysisAiCompleted);
        }

        if (telemetryEvent.PersistenceRequested && !telemetryEvent.PersistenceSucceeded)
        {
            Interlocked.Increment(ref _analysisPersistenceFailures);
        }

        if (telemetryEvent.IsUnmatched)
        {
            Interlocked.Increment(ref _analysisUnmatched);
        }

        Increment(_toolSelections, telemetryEvent.ToolDetected);
        if (!string.IsNullOrWhiteSpace(telemetryEvent.Parser))
        {
            Increment(_parserSelections, telemetryEvent.Parser!);
        }

        foreach (var fingerprint in telemetryEvent.Fingerprints.Where(static value => !string.IsNullOrWhiteSpace(value)))
        {
            Increment(_fingerprints, fingerprint);
        }

        var tags = new TagList
        {
            { "tool", NormalizeName(telemetryEvent.ToolDetected, "unknown") },
            { "input_type", NormalizeName(telemetryEvent.InputType, "unknown") },
            { "parser", NormalizeName(telemetryEvent.Parser, "none") }
        };

        _analysisRequestsCounter.Add(1, tags);
        _analysisDurationMsHistogram.Record(Math.Max(1, telemetryEvent.DurationMs), tags);
        _analysisDiagnosticsHistogram.Record(Math.Max(0, telemetryEvent.DiagnosticsCount), tags);
        _analysisOutcomesCounter.Add(1, CreateTaggedList(tags, "outcome", telemetryEvent.Succeeded ? "success" : "failure"));
        _analysisOutcomesCounter.Add(1, CreateTaggedList(tags, "parse", telemetryEvent.ParseSucceeded ? "success" : "failure"));

        if (telemetryEvent.AiRequested)
        {
            _analysisAiCounter.Add(1, CreateTaggedList(tags, "stage", telemetryEvent.AiCompleted ? "completed" : "requested"));
        }

        if (telemetryEvent.PersistenceRequested)
        {
            _analysisPersistenceCounter.Add(1, CreateTaggedList(tags, "outcome", telemetryEvent.PersistenceSucceeded ? "success" : "failure"));
        }
    }

    public void RecordHttpRequest(HttpRequestTelemetryEvent telemetryEvent)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);

        if (!_options.Enabled)
        {
            return;
        }

        Interlocked.Increment(ref _httpTotalRequests);
        Interlocked.Add(ref _httpTotalDurationMs, Math.Max(1, telemetryEvent.DurationMs));

        Increment(_httpMethods, telemetryEvent.Method);
        Increment(_httpStatusCodes, telemetryEvent.StatusCode.ToString());
        Increment(_httpRoutes, telemetryEvent.Route);

        var tags = new TagList
        {
            { "method", NormalizeName(telemetryEvent.Method, "unknown") },
            { "route", NormalizeName(telemetryEvent.Route, "unknown") },
            { "status_code", telemetryEvent.StatusCode.ToString() }
        };

        _httpRequestsCounter.Add(1, tags);
        _httpDurationMsHistogram.Record(Math.Max(1, telemetryEvent.DurationMs), tags);
    }

    public TelemetrySnapshot GetSnapshot()
    {
        var analysisTotal = Interlocked.Read(ref _analysisTotalRequests);
        var completed = Interlocked.Read(ref _analysisCompleted);
        var httpTotal = Interlocked.Read(ref _httpTotalRequests);

        return new TelemetrySnapshot(
            Enabled: _options.Enabled,
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Analysis: new AnalysisTelemetrySnapshot(
                TotalRequests: analysisTotal,
                Completed: completed,
                Failed: Interlocked.Read(ref _analysisFailed),
                ParseFailures: Interlocked.Read(ref _analysisParseFailures),
                AiRequested: Interlocked.Read(ref _analysisAiRequested),
                AiCompleted: Interlocked.Read(ref _analysisAiCompleted),
                PersistenceFailures: Interlocked.Read(ref _analysisPersistenceFailures),
                UnmatchedAnalyses: Interlocked.Read(ref _analysisUnmatched),
                AverageDurationMs: analysisTotal == 0 ? 0d : (double)Interlocked.Read(ref _analysisTotalDurationMs) / analysisTotal,
                AverageDiagnosticsPerAnalysis: analysisTotal == 0 ? 0d : (double)Interlocked.Read(ref _analysisTotalDiagnostics) / analysisTotal,
                AiRequestRate: analysisTotal == 0 ? 0d : (double)Interlocked.Read(ref _analysisAiRequested) / analysisTotal,
                UnmatchedAnalysisRate: analysisTotal == 0 ? 0d : (double)Interlocked.Read(ref _analysisUnmatched) / analysisTotal,
                ToolSelections: GetTopItems(_toolSelections, int.MaxValue),
                ParserSelections: GetTopItems(_parserSelections, int.MaxValue),
                TopFingerprints: GetTopFingerprintItems(_fingerprints, Math.Max(1, _options.TopFingerprintLimit))),
            Http: new HttpTelemetrySnapshot(
                TotalRequests: httpTotal,
                AverageDurationMs: httpTotal == 0 ? 0d : (double)Interlocked.Read(ref _httpTotalDurationMs) / httpTotal,
                Methods: GetTopItems(_httpMethods, int.MaxValue),
                StatusCodes: GetTopItems(_httpStatusCodes, int.MaxValue),
                Routes: GetTopItems(_httpRoutes, Math.Max(5, _options.TopFingerprintLimit))));
    }

    private static void Increment(ConcurrentDictionary<string, long> counters, string name)
    {
        var normalized = NormalizeName(name, "unknown");
        counters.AddOrUpdate(normalized, 1, static (_, current) => current + 1);
    }

    private static string NormalizeName(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static IReadOnlyList<TelemetryCountItem> GetTopItems(ConcurrentDictionary<string, long> counters, int limit)
        => counters
            .OrderByDescending(static pair => pair.Value)
            .ThenBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(static pair => new TelemetryCountItem(pair.Key, pair.Value))
            .ToArray();

    private static IReadOnlyList<TelemetryFingerprintItem> GetTopFingerprintItems(ConcurrentDictionary<string, long> counters, int limit)
        => counters
            .OrderByDescending(static pair => pair.Value)
            .ThenBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(static pair => new TelemetryFingerprintItem(pair.Key, pair.Value))
            .ToArray();
    private static TagList CreateTaggedList(TagList source, string key, object? value)
    {
        var copy = new TagList();
        foreach (var tag in source)
        {
            copy.Add(tag.Key, tag.Value);
        }

        copy.Add(key, value);
        return copy;
    }
}
