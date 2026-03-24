using System.Diagnostics;

namespace InsightLogger.Application.Abstractions.Telemetry;

public interface IInsightLoggerTelemetry
{
    Activity? StartAnalysisActivity(string inputType, string? correlationId = null);

    void RecordAnalysisCompleted(AnalysisTelemetryEvent telemetryEvent);

    void RecordHttpRequest(HttpRequestTelemetryEvent telemetryEvent);

    TelemetrySnapshot GetSnapshot();
}
