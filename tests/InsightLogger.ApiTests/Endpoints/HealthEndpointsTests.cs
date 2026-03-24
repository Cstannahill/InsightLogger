using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InsightLogger.Contracts.Ai;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Contracts.Health;
using Microsoft.Extensions.Configuration;
using InsightLogger.ApiTests.Infrastructure;
namespace InsightLogger.ApiTests.Endpoints;

public sealed class HealthEndpointsTests : IClassFixture<ApiTestWebApplicationFactory>
{
    private readonly ApiTestWebApplicationFactory _factory;

    public HealthEndpointsTests(ApiTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ShouldReturn_Service_Metadata()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health");
        var payload = await response.Content.ReadFromJsonAsync<GetHealthResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Status.Should().Be("healthy");
        payload.Service.Should().Be("InsightLogger.Api");
        payload.Version.Should().NotBeNullOrWhiteSpace();
        payload.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }


    [Fact]
    public async Task GetTelemetry_ShouldReturn_Analysis_And_Http_Snapshot()
    {
        using var client = _factory.CreateClient();

        var request = new AnalyzeCompilerErrorRequest(
            Tool: "dotnet",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            Options: new AnalyzeRequestOptionsContract(
                Persist: false,
                UseAiEnrichment: false,
                IncludeRawDiagnostics: false,
                IncludeGroups: false,
                IncludeProcessingMetadata: true));

        using var analysisResponse = await client.PostAsJsonAsync("/analyze/compiler-error", request);
        analysisResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await client.GetAsync("/health/telemetry");
        var payload = await response.Content.ReadFromJsonAsync<GetTelemetryResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Enabled.Should().BeTrue();
        payload.Service.Should().Be("InsightLogger.Api");
        payload.Analysis.TotalRequests.Should().BeGreaterThan(0);
        payload.Analysis.ToolSelections.Should().Contain(item => item.Name == "DotNet");
        payload.Analysis.ParserSelections.Should().Contain(item => item.Name == "dotnet-diagnostic-parser-v1");
        payload.Http.TotalRequests.Should().BeGreaterThan(0);
        payload.Http.Routes.Should().Contain(item => item.Name == "/analyze/compiler-error");
    }

    [Fact]
    public async Task GetAiHealth_ShouldReturn_ConfiguredProviderStatuses()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ai:Enabled"] = "true",
                    ["Ai:Providers:ollama:Type"] = "Ollama",
                    ["Ai:Providers:ollama:Enabled"] = "true",
                    ["Ai:Providers:ollama:DefaultModel"] = "qwen3:8b",
                    ["Ai:Providers:ollama:BaseUrl"] = "http://localhost:11434",
                    ["Ai:Providers:ollama:RequiresApiKey"] = "false",
                    ["Ai:Providers:ollama:Capabilities:SupportsStreaming"] = "true",
                    ["Ai:Providers:ollama:Capabilities:SupportsToolCalling"] = "true",
                    ["Ai:Providers:ollama:Capabilities:SupportsJsonMode"] = "true",
                    ["Ai:Providers:ollama:Capabilities:SupportsOpenAiCompatibility"] = "false",
                    ["Ai:Providers:ollama:Capabilities:IsLocal"] = "true",
                    ["Ai:Providers:openrouter:Type"] = "OpenRouter",
                    ["Ai:Providers:openrouter:Enabled"] = "true",
                    ["Ai:Providers:openrouter:DefaultModel"] = "openai/gpt-5-mini",
                    ["Ai:Providers:openrouter:BaseUrl"] = "https://openrouter.ai/api/v1",
                    ["Ai:Providers:openrouter:RequiresApiKey"] = "true"
                });
            });
        });

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health/ai");
        var payload = await response.Content.ReadFromJsonAsync<GetAiHealthResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Enabled.Should().BeTrue();
        payload.Providers.Should().ContainSingle(provider => provider.Name == "ollama" && provider.Status == "healthy");
        payload.Providers.Should().ContainSingle(provider =>
            provider.Name == "openrouter" &&
            provider.Status == "unconfigured" &&
            provider.Reason == "API key is missing.");
    }

    [Fact]
    public async Task GetAiProviders_ShouldReturn_Capability_Metadata()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ai:Enabled"] = "true",
                    ["Ai:Providers:ollama:Type"] = "Ollama",
                    ["Ai:Providers:ollama:Enabled"] = "true",
                    ["Ai:Providers:ollama:DefaultModel"] = "qwen3:8b",
                    ["Ai:Providers:ollama:BaseUrl"] = "http://localhost:11434",
                    ["Ai:Providers:ollama:RequiresApiKey"] = "false",
                    ["Ai:Providers:ollama:Capabilities:SupportsStreaming"] = "true",
                    ["Ai:Providers:ollama:Capabilities:SupportsToolCalling"] = "true",
                    ["Ai:Providers:ollama:Capabilities:SupportsJsonMode"] = "true",
                    ["Ai:Providers:ollama:Capabilities:SupportsOpenAiCompatibility"] = "false",
                    ["Ai:Providers:openrouter:Type"] = "OpenRouter",
                    ["Ai:Providers:openrouter:Enabled"] = "true",
                    ["Ai:Providers:openrouter:DefaultModel"] = "openai/gpt-5-mini",
                    ["Ai:Providers:openrouter:BaseUrl"] = "https://openrouter.ai/api/v1",
                    ["Ai:Providers:openrouter:RequiresApiKey"] = "true",
                    ["Ai:Providers:openrouter:ApiKey"] = "or-secret",
                    ["Ai:Providers:openrouter:Capabilities:SupportsStreaming"] = "true",
                    ["Ai:Providers:openrouter:Capabilities:SupportsToolCalling"] = "true",
                    ["Ai:Providers:openrouter:Capabilities:SupportsJsonMode"] = "true",
                    ["Ai:Providers:openrouter:Capabilities:SupportsOpenAiCompatibility"] = "true"
                });
            });
        });

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/providers/ai");
        var payload = await response.Content.ReadFromJsonAsync<GetAiProvidersResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Items.Should().ContainSingle(item =>
            item.Name == "ollama" &&
            item.Type == "Ollama" &&
            item.Capabilities.SupportsStreaming &&
            item.Capabilities.SupportsToolCalling);
        payload.Items.Should().ContainSingle(item =>
            item.Name == "openrouter" &&
            item.Capabilities.SupportsOpenAiCompatibility &&
            item.DefaultModel == "openai/gpt-5-mini");
    }
}


