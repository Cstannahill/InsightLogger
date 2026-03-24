using InsightLogger.Application.Abstractions.Telemetry;
using InsightLogger.Contracts.Health;

namespace InsightLogger.Api.Mapping;

public static class TelemetryContractMapper
{
    public static GetTelemetryResponse ToContract(TelemetrySnapshot snapshot)
        => new(
            Enabled: snapshot.Enabled,
            Service: "InsightLogger.Api",
            GeneratedAtUtc: snapshot.GeneratedAtUtc,
            Analysis: new AnalysisTelemetrySummaryContract(
                TotalRequests: snapshot.Analysis.TotalRequests,
                Completed: snapshot.Analysis.Completed,
                Failed: snapshot.Analysis.Failed,
                ParseFailures: snapshot.Analysis.ParseFailures,
                AiRequested: snapshot.Analysis.AiRequested,
                AiCompleted: snapshot.Analysis.AiCompleted,
                PersistenceFailures: snapshot.Analysis.PersistenceFailures,
                UnmatchedAnalyses: snapshot.Analysis.UnmatchedAnalyses,
                AverageDurationMs: snapshot.Analysis.AverageDurationMs,
                AverageDiagnosticsPerAnalysis: snapshot.Analysis.AverageDiagnosticsPerAnalysis,
                AiRequestRate: snapshot.Analysis.AiRequestRate,
                UnmatchedAnalysisRate: snapshot.Analysis.UnmatchedAnalysisRate,
                ToolSelections: snapshot.Analysis.ToolSelections.Select(ToContract).ToArray(),
                ParserSelections: snapshot.Analysis.ParserSelections.Select(ToContract).ToArray(),
                TopFingerprints: snapshot.Analysis.TopFingerprints.Select(ToContract).ToArray()),
            Http: new HttpTelemetrySummaryContract(
                TotalRequests: snapshot.Http.TotalRequests,
                AverageDurationMs: snapshot.Http.AverageDurationMs,
                Methods: snapshot.Http.Methods.Select(ToContract).ToArray(),
                StatusCodes: snapshot.Http.StatusCodes.Select(ToContract).ToArray(),
                Routes: snapshot.Http.Routes.Select(ToContract).ToArray()));

    private static TelemetryCountItemContract ToContract(TelemetryCountItem item)
        => new(item.Name, item.Count);

    private static TelemetryFingerprintItemContract ToContract(TelemetryFingerprintItem item)
        => new(item.Fingerprint, item.Count);
}
