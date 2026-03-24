using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InsightLogger.Application.Abstractions.Ai;
using InsightLogger.Infrastructure.Ai;
using Microsoft.Extensions.Options;
using Xunit;

namespace InsightLogger.InfrastructureTests.Ai;

public sealed class ConfiguredAiRootCauseNarrativeGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Use_Configured_Ollama_Provider_And_Parse_Json_Payload()
    {
        var options = CreateOptionsMonitor(new AiOptions
        {
            Enabled = true,
            DefaultProvider = "ollama",
            Features = new AiFeaturesOptions
            {
                RootCauseNarrative = new AiRootCauseNarrativeOptions
                {
                    Enabled = true,
                    Provider = "ollama",
                    TimeoutSeconds = 15,
                    AllowFallback = false
                }
            },
            Providers = new Dictionary<string, AiProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["ollama"] = new()
                {
                    Type = "Ollama",
                    Enabled = true,
                    DefaultModel = "qwen3.5:latest",
                    BaseUrl = "http://localhost:11434",
                    RequiresApiKey = false,
                    Capabilities = new AiProviderCapabilitiesOptions { SupportsJsonMode = true, IsLocal = true }
                }
            }
        });

        IAiProviderCatalog catalog = new ConfiguredAiProviderCatalog(options);
        IAiProviderHealthService healthService = new ConfiguredAiProviderHealthService(catalog);
        var httpClientFactory = new StubHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{" +
                "\"response\": \"{\\\"summary\\\":\\\"The build likely fails from one repeated missing-symbol issue.\\\",\\\"groupSummaries\\\":[\\\"Two diagnostics collapse into one repeated fingerprint group.\\\"],\\\"recommendedNextSteps\\\":[\\\"Fix the first unresolved identifier and rebuild.\\\"]}\"" +
                "}",
                Encoding.UTF8,
                "application/json")
        });

        var generator = new ConfiguredAiRootCauseNarrativeGenerator(httpClientFactory, catalog, healthService, options);

        var result = await generator.GenerateAsync(new RootCauseNarrativeRequest(
            Tool: "dotnet",
            TotalDiagnostics: 2,
            GroupCount: 1,
            ErrorCount: 2,
            WarningCount: 0,
            TopRootCauseTitles: new[] { "Unknown symbol in current context" },
            DeterministicGroupSummaries: new[] { "Unknown symbol in current context: 2 related diagnostics matched fingerprint fp_cs0103_name_missing." },
            DeterministicNextSteps: new[] { "Check the symbol spelling." },
            DeterministicSummary: "The .NET log contains 2 diagnostics grouped into 1 likely issue cluster."));

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("ollama");
        result.Model.Should().Be("qwen3.5:latest");
        result.Summary.Should().Be("The build likely fails from one repeated missing-symbol issue.");
        result.GroupSummaries.Should().ContainSingle();
        result.RecommendedNextSteps.Should().ContainSingle();
    }

    private static IOptionsMonitor<AiOptions> CreateOptionsMonitor(AiOptions options)
        => new TestOptionsMonitor(options);

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name = "")
            => new(new StubHttpMessageHandler(_handler));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
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

            public void Dispose()
            {
            }
        }
    }
}
