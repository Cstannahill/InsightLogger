using InsightLogger.Application.Abstractions.Telemetry;

namespace InsightLogger.Application.Telemetry.Queries;

public interface ITelemetryQueryService
{
    Task<TelemetrySnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
