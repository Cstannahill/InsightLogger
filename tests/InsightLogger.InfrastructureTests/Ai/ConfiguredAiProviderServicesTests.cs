using FluentAssertions;
using InsightLogger.Application.Abstractions.Ai;
using InsightLogger.Infrastructure.Ai;
using Microsoft.Extensions.Options;

namespace InsightLogger.InfrastructureTests.Ai;

public sealed class ConfiguredAiProviderServicesTests
{
    [Fact]
    public async Task ConfiguredCatalog_Should_Map_Dictionary_Providers()
    {
        var options = CreateOptionsMonitor(
            new AiOptions
            {
                Enabled = true,
                Providers = new Dictionary<string, AiProviderOptions>(
                    StringComparer.OrdinalIgnoreCase
                )
                {
                    ["ollama"] = new()
                    {
                        Type = "Ollama",
                        Enabled = true,
                        DefaultModel = "qwen3.5:latest",
                        BaseUrl = "http://localhost:11434",
                        Capabilities = new AiProviderCapabilitiesOptions
                        {
                            SupportsStreaming = true,
                            SupportsToolCalling = true,
                            SupportsJsonMode = true,
                            IsLocal = true,
                        },
                    },
                },
            }
        );

        IAiProviderCatalog catalog = new ConfiguredAiProviderCatalog(options);
        var providers = await catalog.GetProvidersAsync();

        providers.Should().ContainSingle();
        providers[0].Name.Should().Be("ollama");
        providers[0].Capabilities.IsLocal.Should().BeTrue();
    }

    [Fact]
    public async Task HealthService_Should_Report_Unconfigured_When_ApiKey_Is_Required_But_Missing()
    {
        var options = CreateOptionsMonitor(
            new AiOptions
            {
                Enabled = true,
                Providers = new Dictionary<string, AiProviderOptions>(
                    StringComparer.OrdinalIgnoreCase
                )
                {
                    ["openrouter"] = new()
                    {
                        Type = "OpenRouter",
                        Enabled = true,
                        DefaultModel = "stepfun/step-3.5-flash:free",
                        BaseUrl = "https://openrouter.ai/api/v1",
                        RequiresApiKey = true,
                        Capabilities = new AiProviderCapabilitiesOptions
                        {
                            SupportsStreaming = true,
                            SupportsToolCalling = true,
                            SupportsJsonMode = true,
                            SupportsOpenAiCompatibility = true,
                        },
                    },
                },
            }
        );

        IAiProviderCatalog catalog = new ConfiguredAiProviderCatalog(options);
        IAiProviderHealthService healthService = new ConfiguredAiProviderHealthService(catalog);

        var statuses = await healthService.GetProviderHealthAsync();

        statuses.Should().ContainSingle();
        statuses[0].Status.Should().Be("unconfigured");
        statuses[0].Reason.Should().Be("API key is missing.");
    }

    private static IOptionsMonitor<AiOptions> CreateOptionsMonitor(AiOptions options)
    {
        return new TestOptionsMonitor(options);
    }

    private sealed class TestOptionsMonitor : IOptionsMonitor<AiOptions>
    {
        public TestOptionsMonitor(AiOptions currentValue)
        {
            CurrentValue = currentValue;
        }

        public AiOptions CurrentValue { get; }

        public AiOptions Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<AiOptions, string?> listener) => NoopDisposable.Instance;

        private sealed class NoopDisposable : IDisposable
        {
            public static readonly NoopDisposable Instance = new();

            public void Dispose() { }
        }
    }
}
