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

public sealed class ConfiguredAiExplanationEnricherTests
{
    [Fact]
    public async Task EnrichAsync_Should_Use_Configured_Ollama_Provider_And_Parse_Json_Payload()
    {
        var options = CreateOptionsMonitor(new AiOptions
        {
            Enabled = true,
            DefaultProvider = "ollama",
            Features = new AiFeaturesOptions
            {
                ExplanationEnrichment = new AiExplanationEnrichmentOptions
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
                "\"response\": \"{\\\"explanation\\\":\\\"A clearer explanation.\\\",\\\"likelyCauses\\\":[\\\"Typo in identifier\\\",\\\"Missing declaration\\\"],\\\"suggestedFixes\\\":[\\\"Fix the identifier spelling.\\\",\\\"Declare the symbol before use.\\\"]}\"" +
                "}",
                Encoding.UTF8,
                "application/json")
        });

        var enricher = new ConfiguredAiExplanationEnricher(httpClientFactory, catalog, healthService, options);

        var result = await enricher.EnrichAsync(new ExplanationEnrichmentRequest(
            Tool: "dotnet",
            DiagnosticCode: "CS0103",
            Category: "MissingSymbol",
            Title: "Unknown symbol in current context",
            Explanation: "The compiler cannot resolve a referenced name in the current scope.",
            LikelyCauses: new[] { "Typo in variable or member name" },
            SuggestedFixes: new[] { "Check the symbol spelling." },
            Signals: new[] { "diagnostic-code:CS0103" }));

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("ollama");
        result.Model.Should().Be("qwen3.5:latest");
        result.Explanation.Should().Be("A clearer explanation.");
        result.LikelyCauses.Should().ContainInOrder("Typo in identifier", "Missing declaration");
        result.SuggestedFixes.Should().ContainInOrder("Fix the identifier spelling.", "Declare the symbol before use.");
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
