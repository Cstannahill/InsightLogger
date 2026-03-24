using InsightLogger.Application.Abstractions.Telemetry;

namespace InsightLogger.Application.Telemetry.Queries;

public sealed class TelemetryQueryService : ITelemetryQueryService
{
    private readonly IInsightLoggerTelemetry _telemetry;

    public TelemetryQueryService(IInsightLoggerTelemetry telemetry)
    {
        _telemetry = telemetry;
    }

    public Task<TelemetrySnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_telemetry.GetSnapshot());
    }
}
