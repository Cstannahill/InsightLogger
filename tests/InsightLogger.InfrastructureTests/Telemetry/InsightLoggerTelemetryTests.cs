using FluentAssertions;
using InsightLogger.Application.Abstractions.Telemetry;
using InsightLogger.Infrastructure.Telemetry;
using Microsoft.Extensions.Options;

namespace InsightLogger.InfrastructureTests.Telemetry;

public sealed class InsightLoggerTelemetryTests
{
    [Fact]
    public void Records_analysis_and_http_metrics_in_snapshot()
    {
        var telemetry = new InsightLoggerTelemetry(Options.Create(new InsightLoggerTelemetryOptions
        {
            Enabled = true,
            TopFingerprintLimit = 2
        }));

        telemetry.RecordAnalysisCompleted(new AnalysisTelemetryEvent(
            InputType: "BuildLog",
            ToolDetected: "DotNet",
            Parser: "dotnet-diagnostic-parser-v1",
            Succeeded: true,
            ParseSucceeded: true,
            AiRequested: true,
            AiCompleted: true,
            PersistenceRequested: true,
            PersistenceSucceeded: false,
            IsUnmatched: false,
            DurationMs: 42,
            DiagnosticsCount: 3,
            GroupCount: 1,
            RootCauseCandidateCount: 1,
            UnparsedSegmentCount: 0,
            Fingerprints: new[]
            {
                "fp_cs0103_name_missing",
                "fp_cs0103_name_missing",
                "fp_cs8618_non_nullable"
            },
            CorrelationId: "corr_telemetry_001"));

        telemetry.RecordHttpRequest(new HttpRequestTelemetryEvent(
            Method: "POST",
            Route: "/analyze/build-log",
            StatusCode: 200,
            DurationMs: 15,
            CorrelationId: "corr_http_001"));

        var snapshot = telemetry.GetSnapshot();

        snapshot.Enabled.Should().BeTrue();
        snapshot.Analysis.TotalRequests.Should().Be(1);
        snapshot.Analysis.Completed.Should().Be(1);
        snapshot.Analysis.Failed.Should().Be(0);
        snapshot.Analysis.ParseFailures.Should().Be(0);
        snapshot.Analysis.AiRequested.Should().Be(1);
        snapshot.Analysis.AiCompleted.Should().Be(1);
        snapshot.Analysis.PersistenceFailures.Should().Be(1);
        snapshot.Analysis.AverageDiagnosticsPerAnalysis.Should().Be(3);
        snapshot.Analysis.ToolSelections.Should().ContainSingle(item => item.Name == "DotNet" && item.Count == 1);
        snapshot.Analysis.ParserSelections.Should().ContainSingle(item => item.Name == "dotnet-diagnostic-parser-v1" && item.Count == 1);
        snapshot.Analysis.TopFingerprints.Should().HaveCount(2);
        snapshot.Analysis.TopFingerprints[0].Fingerprint.Should().Be("fp_cs0103_name_missing");
        snapshot.Analysis.TopFingerprints[0].Count.Should().Be(2);

        snapshot.Http.TotalRequests.Should().Be(1);
        snapshot.Http.Methods.Should().ContainSingle(item => item.Name == "POST" && item.Count == 1);
        snapshot.Http.StatusCodes.Should().ContainSingle(item => item.Name == "200" && item.Count == 1);
        snapshot.Http.Routes.Should().ContainSingle(item => item.Name == "/analyze/build-log" && item.Count == 1);
    }

    [Fact]
    public void Disabled_telemetry_returns_empty_snapshot()
    {
        var telemetry = new InsightLoggerTelemetry(Options.Create(new InsightLoggerTelemetryOptions
        {
            Enabled = false,
            TopFingerprintLimit = 5
        }));

        telemetry.RecordAnalysisCompleted(new AnalysisTelemetryEvent(
            InputType: "BuildLog",
            ToolDetected: "DotNet",
            Parser: null,
            Succeeded: false,
            ParseSucceeded: false,
            AiRequested: false,
            AiCompleted: false,
            PersistenceRequested: false,
            PersistenceSucceeded: true,
            IsUnmatched: true,
            DurationMs: 5,
            DiagnosticsCount: 0,
            GroupCount: 0,
            RootCauseCandidateCount: 0,
            UnparsedSegmentCount: 0,
            Fingerprints: Array.Empty<string>()));

        var snapshot = telemetry.GetSnapshot();

        snapshot.Enabled.Should().BeFalse();
        snapshot.Analysis.TotalRequests.Should().Be(0);
        snapshot.Http.TotalRequests.Should().Be(0);
    }
}
