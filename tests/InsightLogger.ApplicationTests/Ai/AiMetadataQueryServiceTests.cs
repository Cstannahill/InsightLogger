using FluentAssertions;
using InsightLogger.Application.Abstractions.Ai;
using InsightLogger.Application.Ai.Queries;

namespace InsightLogger.ApplicationTests.Ai;

public sealed class AiMetadataQueryServiceTests
{
    [Fact]
    public async Task GetHealthAsync_Should_Project_Provider_Statuses()
    {
        var catalog = new FakeAiProviderCatalog(
            enabled: true,
            providers:
            [
                new AiProviderDefinition(
                    Name: "ollama",
                    Type: "Ollama",
                    Enabled: true,
                    DefaultModel: "qwen3:8b",
                    BaseUrl: "http://localhost:11434",
                    RequiresApiKey: false,
                    HasApiKey: false,
                    Capabilities: new AiProviderCapabilities(true, true, true, false, true))
            ]);

        var healthService = new FakeAiProviderHealthService(
            [new AiProviderHealthStatus("ollama", "healthy", "qwen3:8b", "Configuration is ready.")]);

        var service = new AiMetadataQueryService(catalog, healthService);
        var result = await service.GetHealthAsync();

        result.Enabled.Should().BeTrue();
        result.Providers.Should().ContainSingle();
        result.Providers[0].Name.Should().Be("ollama");
        result.Providers[0].Status.Should().Be("healthy");
    }

    [Fact]
    public async Task GetProvidersAsync_Should_Project_Capabilities()
    {
        var catalog = new FakeAiProviderCatalog(
            enabled: true,
            providers:
            [
                new AiProviderDefinition(
                    Name: "openrouter",
                    Type: "OpenRouter",
                    Enabled: true,
                    DefaultModel: "openai/gpt-5-mini",
                    BaseUrl: "https://openrouter.ai/api/v1",
                    RequiresApiKey: true,
                    HasApiKey: true,
                    Capabilities: new AiProviderCapabilities(true, true, true, true, false))
            ]);

        var service = new AiMetadataQueryService(catalog, new FakeAiProviderHealthService(Array.Empty<AiProviderHealthStatus>()));
        var result = await service.GetProvidersAsync();

        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("openrouter");
        result.Items[0].Capabilities.SupportsOpenAiCompatibility.Should().BeTrue();
        result.Items[0].Capabilities.IsLocal.Should().BeFalse();
    }

    private sealed class FakeAiProviderCatalog : IAiProviderCatalog
    {
        private readonly bool _enabled;
        private readonly IReadOnlyList<AiProviderDefinition> _providers;

        public FakeAiProviderCatalog(bool enabled, IReadOnlyList<AiProviderDefinition> providers)
        {
            _enabled = enabled;
            _providers = providers;
        }

        public Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_enabled);
        }

        public Task<IReadOnlyList<AiProviderDefinition>> GetProvidersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_providers);
        }
    }

    private sealed class FakeAiProviderHealthService : IAiProviderHealthService
    {
        private readonly IReadOnlyList<AiProviderHealthStatus> _statuses;

        public FakeAiProviderHealthService(IReadOnlyList<AiProviderHealthStatus> statuses)
        {
            _statuses = statuses;
        }

        public Task<IReadOnlyList<AiProviderHealthStatus>> GetProviderHealthAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_statuses);
        }
    }
}
