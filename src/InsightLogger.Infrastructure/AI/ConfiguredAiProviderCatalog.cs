using InsightLogger.Application.Abstractions.Ai;
using Microsoft.Extensions.Options;

namespace InsightLogger.Infrastructure.Ai;

public sealed class ConfiguredAiProviderCatalog : IAiProviderCatalog
{
    private readonly IOptionsMonitor<AiOptions> _optionsMonitor;

    public ConfiguredAiProviderCatalog(IOptionsMonitor<AiOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    public Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_optionsMonitor.CurrentValue.Enabled);
    }

    public Task<IReadOnlyList<AiProviderDefinition>> GetProvidersAsync(CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;

        var providers = options.Providers
            .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static pair => ToDefinition(pair.Key, pair.Value))
            .ToArray();

        return Task.FromResult<IReadOnlyList<AiProviderDefinition>>(providers);
    }

    private static AiProviderDefinition ToDefinition(string name, AiProviderOptions options)
    {
        return new AiProviderDefinition(
            Name: name,
            Type: options.Type,
            Enabled: options.Enabled,
            DefaultModel: options.DefaultModel,
            BaseUrl: options.BaseUrl,
            RequiresApiKey: options.RequiresApiKey,
            HasApiKey: !string.IsNullOrWhiteSpace(options.ApiKey),
            Capabilities: new AiProviderCapabilities(
                SupportsStreaming: options.Capabilities.SupportsStreaming,
                SupportsToolCalling: options.Capabilities.SupportsToolCalling,
                SupportsJsonMode: options.Capabilities.SupportsJsonMode,
                SupportsOpenAiCompatibility: options.Capabilities.SupportsOpenAiCompatibility,
                IsLocal: options.Capabilities.IsLocal));
    }
}
