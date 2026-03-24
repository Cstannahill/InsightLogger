namespace InsightLogger.Application.Abstractions.Ai;

public interface IAiProviderCatalog
{
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiProviderDefinition>> GetProvidersAsync(
        CancellationToken cancellationToken = default
    );
}
