using InsightLogger.Application.Abstractions.Ai;

namespace InsightLogger.Infrastructure.Ai;

public sealed class ConfiguredAiProviderHealthService : IAiProviderHealthService
{
    private readonly IAiProviderCatalog _providerCatalog;

    public ConfiguredAiProviderHealthService(IAiProviderCatalog providerCatalog)
    {
        _providerCatalog = providerCatalog;
    }

    public async Task<IReadOnlyList<AiProviderHealthStatus>> GetProviderHealthAsync(CancellationToken cancellationToken = default)
    {
        var globalEnabled = await _providerCatalog.IsEnabledAsync(cancellationToken);
        var providers = await _providerCatalog.GetProvidersAsync(cancellationToken);

        return providers
            .Select(provider => EvaluateHealth(globalEnabled, provider))
            .ToArray();
    }

    private static AiProviderHealthStatus EvaluateHealth(bool globalEnabled, AiProviderDefinition provider)
    {
        if (!globalEnabled)
        {
            return new AiProviderHealthStatus(
                Name: provider.Name,
                Status: "disabled",
                DefaultModel: provider.DefaultModel,
                Reason: "AI subsystem is disabled.");
        }

        if (!provider.Enabled)
        {
            return new AiProviderHealthStatus(
                Name: provider.Name,
                Status: "disabled",
                DefaultModel: provider.DefaultModel,
                Reason: "Provider is disabled.");
        }

        if (string.IsNullOrWhiteSpace(provider.BaseUrl))
        {
            return new AiProviderHealthStatus(
                Name: provider.Name,
                Status: "unconfigured",
                DefaultModel: provider.DefaultModel,
                Reason: "Base URL is missing.");
        }

        if (provider.RequiresApiKey && !provider.HasApiKey)
        {
            return new AiProviderHealthStatus(
                Name: provider.Name,
                Status: "unconfigured",
                DefaultModel: provider.DefaultModel,
                Reason: "API key is missing.");
        }

        return new AiProviderHealthStatus(
            Name: provider.Name,
            Status: "healthy",
            DefaultModel: provider.DefaultModel,
            Reason: "Configuration is ready.");
    }
}
