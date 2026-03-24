namespace InsightLogger.Application.Abstractions.Ai;

public interface IAiProviderHealthService
{
    Task<IReadOnlyList<AiProviderHealthStatus>> GetProviderHealthAsync(CancellationToken cancellationToken = default);
}
